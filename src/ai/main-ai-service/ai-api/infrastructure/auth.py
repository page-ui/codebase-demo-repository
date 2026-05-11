import os
from dataclasses import dataclass
from typing import Any

import jwt
from flask import Request


@dataclass
class AuthError(Exception):
    message: str
    status_code: int = 401

    def __str__(self) -> str:
        return self.message


def _validate_optional_api_key(request: Request) -> None:
    configured_api_key = os.getenv("AI_API_KEY", "").strip()
    if not configured_api_key:
        return

    provided = request.headers.get("X-AI-Api-Key", "").strip()
    if provided != configured_api_key:
        raise AuthError("Invalid or missing X-AI-Api-Key", 401)


def _decode_claims_if_possible(token: str) -> dict[str, Any]:
    jwt_secret = os.getenv("JWT_SECRET", "").strip()
    issuer = os.getenv("JWT_ISSUER", "Page.Ui.Worker.Ai").strip()
    audience = os.getenv("JWT_AUDIENCE", "AiModelApi").strip()
    algorithm = os.getenv("JWT_ALGORITHM", "HS256").strip()

    if not jwt_secret:
        try:
            return jwt.decode(token, options={"verify_signature": False})
        except Exception:
            return {}

    return jwt.decode(
        token,
        jwt_secret,
        algorithms=[algorithm],
        issuer=issuer or None,
        audience=audience or None,
    )


def extract_and_validate_request_auth(request: Request) -> str:
    _validate_optional_api_key(request)

    auth_header = request.headers.get("Authorization", "")
    if not auth_header.startswith("Bearer "):
        raise AuthError("Missing Bearer token", 401)

    token = auth_header.removeprefix("Bearer ").strip()
    if not token:
        raise AuthError("Empty Bearer token", 401)

    try:
        claims = _decode_claims_if_possible(token)
        if claims:
            print(
                "[auth] token claims: "
                f"sub={claims.get('sub')} "
                f"user_id={claims.get('user_id')} "
                f"chat_id={claims.get('chat_id')} "
                f"message_id={claims.get('message_id')}"
            )
    except jwt.PyJWTError as exc:
        raise AuthError(f"Invalid Bearer token: {exc}", 401) from exc

    return token