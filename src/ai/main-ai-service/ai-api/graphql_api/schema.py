import asyncio
import uuid
from typing import Optional

import strawberry
from strawberry.types import Info

from ai_pipeline import run_ai_pipeline


@strawberry.input
class GenerateInput:
    chat_id: str
    chat_key: str
    user_storage_key: str
    version_id: str
    trigger_message_id: str
    trigger_message_key: Optional[str]
    trigger_message_content: str
    trigger_message_attachment_url: Optional[str] = None
    model_id: Optional[str] = None
    system_prompt: Optional[str] = None
    chat_name: Optional[str] = None
    ui_target: Optional[str] = None


@strawberry.type
class GeneratePayload:
    accepted: bool
    run_id: str


@strawberry.input
class ReportErrorInput:
    chat_id: str
    chat_key: str
    version_id: Optional[str]
    user_id: Optional[str]
    errors: list[str]
    logs: list[str]


@strawberry.type
class Query:
    @strawberry.field
    def health(self) -> str:
        return "ok"


@strawberry.type
class Mutation:
    @strawberry.mutation
    async def generate(self, info: Info, input: GenerateInput) -> GeneratePayload:
        # Manual GraphQL testing endpoint.
        # For production Page.Ui integration, prefer POST /api/generate because it carries the raw Authorization header.
        request = info.context.get("request") if info.context else None
        auth_header = request.headers.get("Authorization", "") if request else ""
        token = auth_header.removeprefix("Bearer ").strip()

        run_id = str(uuid.uuid4())
        payload = {
            "chatId": input.chat_id,
            "chatKey": input.chat_key,
            "userStorageKey": input.user_storage_key,
            "versionId": input.version_id,
            "modelId": input.model_id,
            "systemPrompt": input.system_prompt,
            "chatName": input.chat_name,
            "triggerMessageId": input.trigger_message_id,
            "triggerMessageKey": input.trigger_message_key,
            "triggerMessageContent": input.trigger_message_content,
            "triggerMessageAttachmentUrl": input.trigger_message_attachment_url,
            "ui_target": input.ui_target,
            "type": "USER_MESSAGE",
        }

        asyncio.create_task(run_ai_pipeline(token=token, payload=payload, run_id=run_id))
        return GeneratePayload(accepted=True, run_id=run_id)

    @strawberry.mutation
    def report_error(self, input: ReportErrorInput) -> bool:
        print("[graphql-render-error]", input)
        return True


schema = strawberry.Schema(query=Query, mutation=Mutation)