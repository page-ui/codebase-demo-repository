import os
from typing import Any

from graphql_client.client import GraphQLClient
from graphql_client.enums import MessageType
from graphql_client.mutations import CREATE_MESSAGE_MUTATION, RENAME_CHAT_MUTATION


def _client() -> GraphQLClient:
    endpoint = os.getenv("BACKEND_GRAPHQL_URL", "http://nginx/graphql/")
    return GraphQLClient(endpoint=endpoint)


async def send_message(
    token: str,
    chat_key: str,
    content: str,
    reply_to_key: str | None = None,
    type: MessageType | str = MessageType.AI_MESSAGE,
    attachment_url: str | None = None,
) -> dict[str, Any]:
    message_type = type.value if isinstance(type, MessageType) else str(type)

    variables = {
        "input": {
            "chatKey": chat_key,
            "content": content,
            "replyToKey": reply_to_key,
            "attachmentUrl": attachment_url,
            "type": message_type,
        }
    }

    data = await _client().execute(
        CREATE_MESSAGE_MUTATION,
        variables=variables,
        token=token,
    )
    return data["createMessage"]


async def rename_chat(token: str, chat_key: str, name: str) -> dict[str, Any]:
    variables = {
        "input": {
            "chatKey": chat_key,
            "name": name,
        }
    }

    data = await _client().execute(
        RENAME_CHAT_MUTATION,
        variables=variables,
        token=token,
    )
    return data["renameChat"]