import os
from typing import Any

import httpx

from infrastructure.urls import join_base_url


def _backend_base_url() -> str:
    return os.getenv("BACKEND_BASE_URL", "http://nginx").rstrip("/")


async def get_presigned_url(
    token: str,
    user_storage_key: str,
    chat_key: str,
    version_id: str,
    file_name: str,
) -> dict[str, Any]:
    url = join_base_url(_backend_base_url(), "/api/ai-dev/upload/presign")
    headers = {"Authorization": f"Bearer {token}"}
    params = {
        "userStorageKey": user_storage_key,
        "chatKey": chat_key,
        "versionId": version_id,
        "fileName": file_name,
    }

    async with httpx.AsyncClient(timeout=60.0) as client:
        response = await client.get(url, params=params, headers=headers)

    response.raise_for_status()
    return response.json()


async def upload_file(upload_url: str, content: bytes, content_type: str) -> None:
    url = join_base_url(_backend_base_url(), upload_url)
    headers = {"Content-Type": content_type}

    async with httpx.AsyncClient(timeout=120.0) as client:
        response = await client.put(url, content=content, headers=headers)

    response.raise_for_status()