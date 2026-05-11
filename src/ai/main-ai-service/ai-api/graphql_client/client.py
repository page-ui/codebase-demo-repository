from typing import Any

import httpx


class GraphQLError(RuntimeError):
    pass


class GraphQLClient:
    def __init__(self, endpoint: str):
        self.endpoint = endpoint

    async def execute(
        self,
        query: str,
        variables: dict[str, Any] | None = None,
        token: str | None = None,
    ) -> dict[str, Any]:
        headers = {"Content-Type": "application/json"}
        if token:
            headers["Authorization"] = f"Bearer {token}"

        async with httpx.AsyncClient(timeout=60.0) as client:
            response = await client.post(
                self.endpoint,
                json={"query": query, "variables": variables or {}},
                headers=headers,
            )

        response.raise_for_status()
        payload = response.json()

        errors = payload.get("errors")
        if errors:
            raise GraphQLError(str(errors))

        data = payload.get("data")
        if data is None:
            raise GraphQLError(f"GraphQL response has no data: {payload}")

        return data