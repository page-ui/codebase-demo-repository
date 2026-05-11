import asyncio
import os
import threading
import uuid
from typing import Any

from dotenv import load_dotenv
from flask import Flask, jsonify, request
from strawberry.flask.views import GraphQLView

from ai_pipeline import run_ai_pipeline
from diagnostics_graphql.schema import schema as diagnostics_schema
from infrastructure.auth import AuthError, extract_and_validate_request_auth

load_dotenv()

app = Flask(__name__)

worker_loop = asyncio.new_event_loop()


def _start_worker_loop() -> None:
    asyncio.set_event_loop(worker_loop)
    worker_loop.run_forever()


threading.Thread(target=_start_worker_loop, daemon=True).start()


def submit_background(coro) -> None:
    asyncio.run_coroutine_threadsafe(coro, worker_loop)


@app.get("/health")
def health():
    return jsonify({"status": "ok", "service": "page-ui-ai-api"})


@app.post("/api/generate")
def generate():
    """
    Called by the .NET backend.

    Must return quickly with 202.
    The long generation continues in the background.
    """
    try:
        token = extract_and_validate_request_auth(request)
    except AuthError as exc:
        return jsonify({"error": str(exc)}), exc.status_code

    payload: dict[str, Any] = request.get_json(silent=True) or {}

    required_fields = [
        "chatId",
        "chatKey",
        "userStorageKey",
        "versionId",
        "triggerMessageId",
        "triggerMessageContent",
    ]
    missing = [field for field in required_fields if not payload.get(field)]
    if missing:
        return jsonify({"error": f"Missing required fields: {', '.join(missing)}"}), 400

    run_id = str(uuid.uuid4())

    submit_background(run_ai_pipeline(token=token, payload=payload, run_id=run_id))

    return jsonify({"accepted": True, "runId": run_id}), 202


@app.post("/api/report-error")
def report_error():
    """
    Called by the .NET backend if rendered files fail in browser/runtime.
    """
    try:
        extract_and_validate_request_auth(request)
    except AuthError as exc:
        return jsonify({"error": str(exc)}), exc.status_code

    payload = request.get_json(silent=True) or {}
    print("[render-error]", payload)

    return jsonify({"accepted": True}), 202


# Optional Strawberry endpoint for diagnostics only.
# This is NOT the .NET backend schema.
app.add_url_rule(
    "/graphql",
    view_func=GraphQLView.as_view("diagnostics_graphql", schema=diagnostics_schema),
)


if __name__ == "__main__":
    port = int(os.getenv("PORT", "5000"))
    app.run(host="0.0.0.0", port=port, debug=False, threaded=True)