from uuid import UUID

import httpx
import pytest

from clarive.client import ClariveClientSync
from clarive.exceptions import ClariveNotFoundError, ClariveValidationError
from clarive.models import GenerateRequest

ENTRY_ID = UUID("3fa85f64-5717-4562-b3fc-2c963f66afa6")

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
    "tags": ["test"],
    "updatedAt": "2026-03-18T10:00:00Z",
    "publishedAt": "2026-03-18T10:00:00Z",
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


def _make_transport(response_json: dict, status_code: int = 200) -> httpx.MockTransport:
    def handler(request: httpx.Request) -> httpx.Response:
        return httpx.Response(status_code, json=response_json)

    return httpx.MockTransport(handler)


class TestClariveClientSyncGetEntry:
    def test_sends_get_and_returns_entry(self) -> None:
        transport = _make_transport(ENTRY_RESPONSE)
        with ClariveClientSync(api_key="cl_testkey") as client:
            client._client = httpx.Client(
                base_url=str(client._client.base_url),
                headers=dict(client._client.headers),
                transport=transport,
            )
            entry = client.get_entry(ENTRY_ID)

        assert entry.id == ENTRY_ID
        assert entry.title == "Test Entry"
        assert len(entry.prompts) == 1

    def test_raises_on_404(self) -> None:
        transport = _make_transport(
            {"error": {"code": "NOT_FOUND", "message": "Not found"}},
            status_code=404,
        )
        with ClariveClientSync(api_key="cl_testkey") as client:
            client._client = httpx.Client(
                base_url=str(client._client.base_url),
                headers=dict(client._client.headers),
                transport=transport,
            )
            with pytest.raises(ClariveNotFoundError) as exc_info:
                client.get_entry(ENTRY_ID)
            assert exc_info.value.status_code == 404


class TestClariveClientSyncGenerate:
    def test_sends_post_and_returns_response(self) -> None:
        transport = _make_transport(GENERATE_RESPONSE)
        with ClariveClientSync(api_key="cl_testkey") as client:
            client._client = httpx.Client(
                base_url=str(client._client.base_url),
                headers=dict(client._client.headers),
                transport=transport,
            )
            result = client.generate(ENTRY_ID, GenerateRequest(fields={"name": "Alice"}))

        assert result.title == "Test Entry"
        assert result.rendered_prompts[0].content == "Hello Alice"

    def test_raises_on_422(self) -> None:
        transport = _make_transport(
            {"error": {"code": "VALIDATION_ERROR", "message": "Failed", "details": {}}},
            status_code=422,
        )
        with ClariveClientSync(api_key="cl_testkey") as client:
            client._client = httpx.Client(
                base_url=str(client._client.base_url),
                headers=dict(client._client.headers),
                transport=transport,
            )
            with pytest.raises(ClariveValidationError) as exc_info:
                client.generate(ENTRY_ID, GenerateRequest(fields={}))
            assert exc_info.value.details == {}


class TestClariveClientSyncLifecycle:
    def test_context_manager_closes_client(self) -> None:
        client = ClariveClientSync(api_key="cl_testkey")
        with client:
            assert client._client.is_closed is False
        assert client._client.is_closed is True

    def test_manual_close(self) -> None:
        client = ClariveClientSync(api_key="cl_testkey")
        assert client._client.is_closed is False
        client.close()
        assert client._client.is_closed is True
