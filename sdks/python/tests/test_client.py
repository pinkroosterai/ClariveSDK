from uuid import UUID

import httpx
import pytest
import respx

from clarive.client import ClariveClient
from clarive.exceptions import (
    ClariveApiError,
    ClariveNotFoundError,
    ClariveValidationError,
)
from clarive.models import GenerateRequest

ENTRY_ID = UUID("3fa85f64-5717-4562-b3fc-2c963f66afa6")
BASE_URL = "https://app.clarive.com/public/v1/"

ENTRY_RESPONSE = {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "title": "Test Entry",
    "systemMessage": None,
    "version": 1,
    "prompts": [
        {
            "content": "Hello {{name}}",
            "order": 1,
            "isTemplate": True,
            "templateFields": [
                {
                    "id": "00000000-0000-0000-0000-000000000001",
                    "promptId": "00000000-0000-0000-0000-000000000002",
                    "name": "name",
                    "type": "string",
                    "enumValues": None,
                    "defaultValue": None,
                    "min": None,
                    "max": None,
                }
            ],
        }
    ],
}

GENERATE_RESPONSE = {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "title": "Test Entry",
    "version": 1,
    "systemMessage": None,
    "renderedPrompts": [
        {
            "content": "Hello Alice",
            "order": 1,
        }
    ],
}


@pytest.mark.asyncio
class TestClariveClientGetEntry:
    @respx.mock
    async def test_sends_get_with_api_key(self) -> None:
        route = respx.get(f"{BASE_URL}entries/{ENTRY_ID}").mock(
            return_value=httpx.Response(200, json=ENTRY_RESPONSE)
        )

        async with ClariveClient(api_key="cl_testkey") as client:
            entry = await client.get_entry(ENTRY_ID)

        assert route.called
        request = route.calls[0].request
        assert request.headers["X-Api-Key"] == "cl_testkey"

        assert entry.id == ENTRY_ID
        assert entry.title == "Test Entry"
        assert len(entry.prompts) == 1

    @respx.mock
    async def test_raises_on_404(self) -> None:
        error_body = {"error": {"code": "NOT_FOUND", "message": "Not found"}}
        respx.get(f"{BASE_URL}entries/{ENTRY_ID}").mock(
            return_value=httpx.Response(404, json=error_body)
        )

        async with ClariveClient(api_key="cl_testkey") as client:
            with pytest.raises(ClariveNotFoundError) as exc_info:
                await client.get_entry(ENTRY_ID)
            assert exc_info.value.status_code == 404
            assert exc_info.value.error_code == "NOT_FOUND"

    @respx.mock
    async def test_raises_base_error_on_non_json_body(self) -> None:
        respx.get(f"{BASE_URL}entries/{ENTRY_ID}").mock(
            return_value=httpx.Response(500, text="Internal Server Error")
        )

        async with ClariveClient(api_key="cl_testkey") as client:
            with pytest.raises(ClariveApiError) as exc_info:
                await client.get_entry(ENTRY_ID)
            assert exc_info.value.error_code == "UNKNOWN"
            assert exc_info.value.status_code == 500


@pytest.mark.asyncio
class TestClariveClientGenerate:
    @respx.mock
    async def test_sends_post_with_json_body(self) -> None:
        route = respx.post(f"{BASE_URL}entries/{ENTRY_ID}/generate").mock(
            return_value=httpx.Response(200, json=GENERATE_RESPONSE)
        )

        request = GenerateRequest(fields={"name": "Alice"})
        async with ClariveClient(api_key="cl_testkey") as client:
            result = await client.generate(ENTRY_ID, request)

        assert route.called
        sent_request = route.calls[0].request
        assert sent_request.headers["X-Api-Key"] == "cl_testkey"
        assert b'"fields"' in sent_request.content

        assert result.title == "Test Entry"
        assert result.rendered_prompts[0].content == "Hello Alice"

    @respx.mock
    async def test_raises_on_422(self) -> None:
        error_body = {
            "error": {
                "code": "VALIDATION_ERROR",
                "message": "Validation failed",
                "details": {"name": "Required"},
            }
        }
        respx.post(f"{BASE_URL}entries/{ENTRY_ID}/generate").mock(
            return_value=httpx.Response(422, json=error_body)
        )

        async with ClariveClient(api_key="cl_testkey") as client:
            with pytest.raises(ClariveValidationError) as exc_info:
                await client.generate(ENTRY_ID, GenerateRequest(fields={}))
            assert exc_info.value.details == {"name": "Required"}


@pytest.mark.asyncio
class TestClariveClientLifecycle:
    async def test_context_manager_closes_client(self) -> None:
        client = ClariveClient(api_key="cl_testkey")
        async with client:
            assert client._client.is_closed is False
        assert client._client.is_closed is True

    async def test_manual_aclose(self) -> None:
        client = ClariveClient(api_key="cl_testkey")
        assert client._client.is_closed is False
        await client.aclose()
        assert client._client.is_closed is True
