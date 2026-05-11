import os
from typing import Any

import httpx

from infrastructure.urls import join_base_url


def _backend_base_url() -> str:
    return os.getenv("BACKEND_BASE_URL", "http://nginx").rstrip("/")


async def trigger_render(token: str, payload: dict[str, Any]) -> None:
    url = join_base_url(_backend_base_url(), "/api/ai-dev/render-trigger")
    headers = {
        "Authorization": f"Bearer {token}",
        "Content-Type": "application/json",
    }

    async with httpx.AsyncClient(timeout=120.0) as client:
        response = await client.post(url, json=payload, headers=headers)

    if response.status_code not in {200, 202, 204}:
        print(f"[render-trigger] unexpected response {response.status_code}: {response.text}")

    response.raise_for_status()