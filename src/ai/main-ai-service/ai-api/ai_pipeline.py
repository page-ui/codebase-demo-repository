import asyncio
import mimetypes
import os
from pathlib import PurePath
from typing import Any

import httpx
from dotenv import load_dotenv

from graphql_client.enums import MessageType
from infrastructure.graphql_gateway import rename_chat, send_message
from infrastructure.render_gateway import trigger_render
from infrastructure.storage_gateway import get_presigned_url, upload_file

load_dotenv()

DEFAULT_HTML_FILE_NAME = "001-index.html"
KAGGLE_REQUEST_HEADERS = {"ngrok-skip-browser-warning": "true"}
TRANSIENT_KAGGLE_STATUS_CODES = {502, 503, 504}

UI_JOB_START_TIMEOUT_SECONDS = float(os.getenv("UI_JOB_START_TIMEOUT_SECONDS", "60"))
UI_JOB_POLL_INTERVAL_SECONDS = float(os.getenv("UI_JOB_POLL_INTERVAL_SECONDS", "15"))
UI_JOB_TOTAL_TIMEOUT_SECONDS = float(os.getenv("UI_JOB_TOTAL_TIMEOUT_SECONDS", str(2 * 60 * 60)))
UI_JOB_TRANSIENT_POLL_FAILURE_LIMIT = int(os.getenv("UI_JOB_TRANSIENT_POLL_FAILURE_LIMIT", "20"))
UI_AI_SERVICE_URL = os.getenv("UI_AI_SERVICE_URL", "").rstrip("/")


def _content_type_for(file_name: str, explicit: str | None = None) -> str:
    if explicit:
        return explicit
    guessed, _ = mimetypes.guess_type(file_name)
    if file_name.endswith(".js"):
        return "application/javascript"
    if file_name.endswith(".css"):
        return "text/css"
    if file_name.endswith(".html"):
        return "text/html"
    return guessed or "application/octet-stream"


def _basename(file_name: str) -> str:
    return PurePath(file_name.replace("\\", "/")).name or DEFAULT_HTML_FILE_NAME


def _response_preview(response: httpx.Response, limit: int = 500) -> str:
    body = response.text.strip().replace("\n", " ")
    if len(body) > limit:
        return f"{body[:limit]}..."
    return body


def _remaining_timeout(deadline: float, minimum: float = 1.0) -> float:
    return max(minimum, deadline - asyncio.get_running_loop().time())


async def _post_json_with_status_retries(http, url, payload, timeout, attempts=3):
    last_response = None
    for attempt in range(1, attempts + 1):
        response = await http.post(url, json=payload, headers=KAGGLE_REQUEST_HEADERS, timeout=timeout)
        last_response = response
        if response.status_code not in TRANSIENT_KAGGLE_STATUS_CODES:
            response.raise_for_status()
            return response
        preview = _response_preview(response)
        print(f"[DEBUG] Transient Kaggle POST {response.status_code} attempt {attempt}/{attempts}: {preview}")
        if attempt < attempts:
            await asyncio.sleep(10 * attempt)
    last_response.raise_for_status()
    return last_response


async def _get_with_status_retries(http, url, timeout, attempts=3):
    last_response = None
    for attempt in range(1, attempts + 1):
        response = await http.get(url, headers=KAGGLE_REQUEST_HEADERS, timeout=timeout)
        last_response = response
        if response.status_code not in TRANSIENT_KAGGLE_STATUS_CODES:
            response.raise_for_status()
            return response
        preview = _response_preview(response)
        print(f"[DEBUG] Transient Kaggle GET {response.status_code} attempt {attempt}/{attempts}: {preview}")
        if attempt < attempts:
            await asyncio.sleep(10 * attempt)
    last_response.raise_for_status()
    return last_response


def _normalize_generated_files(data: dict[str, Any]) -> list[dict[str, Any]]:
    files = data.get("files")
    normalized: list[dict[str, Any]] = []

    if isinstance(files, list):
        for index, item in enumerate(files, start=1):
            if not isinstance(item, dict):
                continue
            raw_name = item.get("fileName") or item.get("filename") or item.get("name") or f"{index:03d}-file.txt"
            content = item.get("content")
            if content is None:
                content = item.get("text")
            if content is None:
                continue

            file_name = _basename(str(raw_name))
            normalized.append(
                {
                    "fileName": file_name,
                    "contentType": _content_type_for(file_name, item.get("contentType") or item.get("mimeType")),
                    "content": str(content).encode("utf-8"),
                }
            )

    html = data.get("html")
    if html and not any(file["fileName"].endswith(".html") for file in normalized):
        normalized.insert(
            0,
            {
                "fileName": DEFAULT_HTML_FILE_NAME,
                "contentType": "text/html",
                "content": str(html).encode("utf-8"),
            },
        )

    return normalized


async def _poll_ui_generation_job(http, kaggle_api_url: str, job_id: str, poll_url: str | None = None) -> dict[str, Any]:
    loop = asyncio.get_running_loop()
    deadline = loop.time() + UI_JOB_TOTAL_TIMEOUT_SECONDS
    transient_failures = 0

    status_url = poll_url if poll_url and poll_url.startswith("http") else f"{kaggle_api_url}{poll_url or f'/generate/ui/{job_id}'}"
    result_url = f"{kaggle_api_url}/generate/ui/{job_id}/result"

    while True:
        if loop.time() >= deadline:
            raise TimeoutError(f"UI generation job {job_id} exceeded {int(UI_JOB_TOTAL_TIMEOUT_SECONDS)} seconds")

        timeout = httpx.Timeout(connect=10.0, read=min(60.0, _remaining_timeout(deadline)), write=10.0, pool=5.0)

        try:
            response = await _get_with_status_retries(http, status_url, timeout=timeout, attempts=3)
            transient_failures = 0
        except httpx.HTTPStatusError as exc:
            if exc.response.status_code in TRANSIENT_KAGGLE_STATUS_CODES:
                transient_failures += 1
                if transient_failures <= UI_JOB_TRANSIENT_POLL_FAILURE_LIMIT:
                    await asyncio.sleep(UI_JOB_POLL_INTERVAL_SECONDS)
                    continue
            raise

        data = response.json()
        status = str(data.get("status", "")).lower()
        print(f"[DEBUG] Kaggle job {job_id} status={status}")

        if status == "done":
            result = data.get("result")
            if isinstance(result, dict):
                return result
            result_response = await _get_with_status_retries(http, result_url, timeout=timeout, attempts=3)
            result_data = result_response.json()
            if isinstance(result_data, dict) and result_data.get("html"):
                return result_data
            raise ValueError(f"Kaggle job {job_id} finished without a valid result")

        if status in {"failed", "timeout", "cancelled"}:
            raise RuntimeError(f"Kaggle UI job {job_id} ended with status {status}: {data.get('message')}")

        if status not in {"queued", "running"}:
            raise RuntimeError(f"Unknown Kaggle job status {status}: {data}")

        await asyncio.sleep(UI_JOB_POLL_INTERVAL_SECONDS)


async def _request_ui_generation_with_polling(http, kaggle_api_url: str, payload: dict[str, Any]) -> dict[str, Any]:
    response = await _post_json_with_status_retries(
        http,
        f"{kaggle_api_url}/generate/ui",
        payload,
        timeout=httpx.Timeout(connect=10.0, read=UI_JOB_START_TIMEOUT_SECONDS, write=30.0, pool=5.0),
        attempts=3,
    )
    data = response.json()

    if isinstance(data, dict) and data.get("html"):
        return data

    job_id = data.get("job_id")
    if not job_id:
        raise ValueError(f"Kaggle /generate/ui did not return job_id: {data}")

    return await _poll_ui_generation_job(http, kaggle_api_url, str(job_id), data.get("poll_url"))


async def _send_failure_message(token: str, chat_key: str | None, reply_to_key: str | None, content: str) -> None:
    if not chat_key:
        return
    try:
        await send_message(
            token=token,
            chat_key=chat_key,
            content=content,
            reply_to_key=reply_to_key,
            type=MessageType.AI_MESSAGE,
        )
    except Exception as exc:
        print(f"[ERROR] Failed to send failure message: {exc}")


async def _upload_generated_files(token: str, payload: dict[str, Any], files: list[dict[str, Any]]) -> list[dict[str, str]]:
    uploaded_files = []

    for file in files:
        file_name = file["fileName"]
        presign = await get_presigned_url(
            token=token,
            user_storage_key=payload["userStorageKey"],
            chat_key=payload["chatKey"],
            version_id=payload["versionId"],
            file_name=file_name,
        )
        object_key = presign["objectKey"]

        if _basename(object_key) != file_name:
            raise ValueError(f"Presigned object key basename does not match file name: {file_name}")

        await upload_file(presign["uploadUrl"], file["content"], file["contentType"])

        uploaded_files.append(
            {
                "fileName": file_name,
                "contentType": file["contentType"],
                "objectKey": object_key,
            }
        )

    return uploaded_files


async def run_ai_pipeline(token: str, payload: dict[str, Any], run_id: str) -> None:
    kaggle_api_url = os.getenv("KAGGLE_API_URL", "").rstrip("/")

    chat_key = payload.get("chatKey")
    prompt = payload.get("triggerMessageContent", "").strip()
    ui_target = payload.get("ui_target")
    reply_to_key = payload.get("triggerMessageKey")
    trigger_message_id = payload.get("triggerMessageId")
    attachment_url = payload.get("triggerMessageAttachmentUrl")

    if not kaggle_api_url:
        await _send_failure_message(token, chat_key, reply_to_key, "UI generation failed: KAGGLE_API_URL is not configured.")
        return

    print(f"[DEBUG] Starting AI run {run_id} for chat {chat_key}")

    session_title = ""
    detected_target = ui_target

    async with httpx.AsyncClient(timeout=None) as http:
        try:
            title_response = await http.post(
                f"{kaggle_api_url}/generate/title",
                json={"prompt": prompt},
                headers=KAGGLE_REQUEST_HEADERS,
                timeout=httpx.Timeout(connect=10.0, read=60.0, write=10.0, pool=5.0),
            )
            title_response.raise_for_status()
            title_data = title_response.json()
            session_title = title_data.get("title", "").strip()
            detected_target = title_data.get("ui_target", ui_target)

            if session_title:
                await rename_chat(token=token, chat_key=chat_key, name=session_title)
        except Exception as exc:
            print(f"[DEBUG] Title step failed; continuing: {exc}")

        ui_analysis = None
        if attachment_url and UI_AI_SERVICE_URL:
            try:
                print(f"[DEBUG] Analyzing UI attachment: {attachment_url}")
                analysis_response = await http.post(
                    f"{UI_AI_SERVICE_URL}/ai/analyze-ui",
                    json={"imageUrl": attachment_url},
                    timeout=httpx.Timeout(connect=10.0, read=120.0, write=10.0, pool=5.0),
                )
                analysis_response.raise_for_status()
                ui_analysis = analysis_response.json()
                print(f"[DEBUG] UI analysis completed: {len(ui_analysis.get('elements', []))} elements found")
            except Exception as exc:
                print(f"[DEBUG] UI analysis failed; continuing: {exc}")

        try:
            effective_target = ui_target or detected_target
            ui_payload = {"prompt": prompt}
            if effective_target:
                ui_payload["ui_target"] = effective_target
            if attachment_url:
                ui_payload["attachmentUrl"] = attachment_url
            if ui_analysis:
                ui_payload["ui_analysis"] = ui_analysis

            data = await _request_ui_generation_with_polling(http, kaggle_api_url, ui_payload)

            generated_files = _normalize_generated_files(data)
            if not generated_files:
                raise ValueError("UI pipeline returned no source files or HTML")

            uploaded_files = await _upload_generated_files(token, payload, generated_files)

            await trigger_render(
                token=token,
                payload={
                    "chatId": payload["chatId"],
                    "chatKey": payload["chatKey"],
                    "replyToMessageId": trigger_message_id,
                    "runId": run_id,
                    "versionId": payload["versionId"],
                    "userStorageKey": payload["userStorageKey"],
                    "files": uploaded_files,
                },
            )

            app_name = data.get("title", session_title).strip()
            audit = data.get("audit", {})
            score = audit.get("score", "N/A") if isinstance(audit, dict) else "N/A"

            print(f"[DEBUG] Render triggered for run {run_id}; files={len(uploaded_files)}, app={app_name!r}, score={score}")

            if app_name and app_name != session_title:
                await rename_chat(token=token, chat_key=chat_key, name=app_name)

        except (httpx.TimeoutException, asyncio.TimeoutError, TimeoutError) as exc:
            print(f"[ERROR] UI generation timed out: {exc}")
            await _send_failure_message(
                token,
                chat_key,
                reply_to_key,
                "UI generation timed out after 2 hours. Please try again with a smaller prompt or restart the Kaggle session.",
            )
        except Exception as exc:
            print(f"[ERROR] UI pipeline exception: {exc}")
            await _send_failure_message(token, chat_key, reply_to_key, f"UI generation failed: {exc}")