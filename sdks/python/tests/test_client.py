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
from clarive.models import (
    EntrySummary,
    GenerateRequest,
    ListEntriesOptions,
    PaginatedResponse,
    TabSummary,
    TagInfo,
)

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
    "tags": ["test"],
    "updatedAt": "2026-03-18T10:00:00Z",
    "publishedAt": "2026-03-18T10:00:00Z",
    "tabs": [
        {
            "id": "00000000-0000-0000-0000-000000000010",
            "name": "Main",
            "isMainTab": True,
            "forkedFromVersion": None,
        }
    ],
    "tabCount": 1,
}

LIST_ENTRIES_RESPONSE = {
    "items": [
        {
            "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
            "title": "Test Entry",
            "version": 1,
            "hasSystemMessage": True,
            "isTemplate": True,
            "isChain": False,
            "promptCount": 1,
            "firstPromptPreview": "Hello {{name}}",
            "tags": ["test"],
            "createdAt": "2026-03-18T10:00:00Z",
            "updatedAt": "2026-03-18T10:00:00Z",
            "tabs": [],
            "tabCount": 0,
        }
    ],
    "totalCount": 1,
    "page": 1,
    "pageSize": 50,
}

LIST_TAGS_RESPONSE = [
    {"name": "test", "entryCount": 5},
    {"name": "production", "entryCount": 3},
]

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


@pytest.mark.asyncio
class TestClariveClientListEntries:
    @respx.mock
    async def test_list_entries_returns_paginated_response(self) -> None:
        route = respx.get(f"{BASE_URL}entries").mock(
            return_value=httpx.Response(200, json=LIST_ENTRIES_RESPONSE)
        )

        async with ClariveClient(api_key="cl_testkey") as client:
            result = await client.list_entries()

        assert route.called
        assert isinstance(result, PaginatedResponse)
        assert result.total_count == 1
        assert result.page == 1
        assert result.page_size == 50
        assert len(result.items) == 1

        item = result.items[0]
        assert isinstance(item, EntrySummary)
        assert item.id == ENTRY_ID
        assert item.title == "Test Entry"
        assert item.has_system_message is True
        assert item.is_template is True
        assert item.is_chain is False
        assert item.prompt_count == 1
        assert item.first_prompt_preview == "Hello {{name}}"
        assert item.tags == ["test"]

    @respx.mock
    async def test_list_entries_passes_query_params(self) -> None:
        route = respx.get(url__regex=rf"{BASE_URL}entries\?.*").mock(
            return_value=httpx.Response(200, json=LIST_ENTRIES_RESPONSE)
        )

        options = ListEntriesOptions(page=2, search="test")
        async with ClariveClient(api_key="cl_testkey") as client:
            await client.list_entries(options)

        assert route.called
        request_url = str(route.calls[0].request.url)
        assert "page=2" in request_url
        assert "search=test" in request_url


@pytest.mark.asyncio
class TestClariveClientListTags:
    @respx.mock
    async def test_list_tags_returns_tag_list(self) -> None:
        route = respx.get(f"{BASE_URL}tags").mock(
            return_value=httpx.Response(200, json=LIST_TAGS_RESPONSE)
        )

        async with ClariveClient(api_key="cl_testkey") as client:
            result = await client.list_tags()

        assert route.called
        assert len(result) == 2
        assert all(isinstance(t, TagInfo) for t in result)
        assert result[0].name == "test"
        assert result[0].entry_count == 5
        assert result[1].name == "production"
        assert result[1].entry_count == 3


TAB_ID = UUID("00000000-0000-0000-0000-000000000010")

LIST_TABS_RESPONSE = [
    {
        "id": "00000000-0000-0000-0000-000000000010",
        "name": "Main",
        "isMainTab": True,
        "forkedFromVersion": None,
    },
    {
        "id": "00000000-0000-0000-0000-000000000011",
        "name": "Formal",
        "isMainTab": False,
        "forkedFromVersion": 3,
    },
]


@pytest.mark.asyncio
class TestClariveClientListTabs:
    @respx.mock
    async def test_list_tabs_returns_tab_list(self) -> None:
        route = respx.get(f"{BASE_URL}entries/{ENTRY_ID}/tabs").mock(
            return_value=httpx.Response(200, json=LIST_TABS_RESPONSE)
        )

        async with ClariveClient(api_key="cl_testkey") as client:
            result = await client.list_tabs(ENTRY_ID)

        assert route.called
        assert len(result) == 2
        assert all(isinstance(t, TabSummary) for t in result)
        assert result[0].name == "Main"
        assert result[0].is_main_tab is True
        assert result[1].name == "Formal"
        assert result[1].forked_from_version == 3


@pytest.mark.asyncio
class TestClariveClientGetTab:
    @respx.mock
    async def test_get_tab_returns_prompt_entry(self) -> None:
        route = respx.get(f"{BASE_URL}entries/{ENTRY_ID}/tabs/{TAB_ID}").mock(
            return_value=httpx.Response(200, json=ENTRY_RESPONSE)
        )

        async with ClariveClient(api_key="cl_testkey") as client:
            entry = await client.get_tab(ENTRY_ID, TAB_ID)

        assert route.called
        assert entry.id == ENTRY_ID
        assert entry.title == "Test Entry"

    @respx.mock
    async def test_get_tab_raises_on_404(self) -> None:
        error_body = {"error": {"code": "TAB_NOT_FOUND", "message": "Tab not found."}}
        respx.get(f"{BASE_URL}entries/{ENTRY_ID}/tabs/{TAB_ID}").mock(
            return_value=httpx.Response(404, json=error_body)
        )

        async with ClariveClient(api_key="cl_testkey") as client:
            with pytest.raises(ClariveNotFoundError):
                await client.get_tab(ENTRY_ID, TAB_ID)


@pytest.mark.asyncio
class TestClariveClientGenerateTab:
    @respx.mock
    async def test_generate_tab_sends_post(self) -> None:
        route = respx.post(f"{BASE_URL}entries/{ENTRY_ID}/tabs/{TAB_ID}/generate").mock(
            return_value=httpx.Response(200, json=GENERATE_RESPONSE)
        )

        request = GenerateRequest(fields={"name": "Alice"})
        async with ClariveClient(api_key="cl_testkey") as client:
            result = await client.generate_tab(ENTRY_ID, TAB_ID, request)

        assert route.called
        assert result.rendered_prompts[0].content == "Hello Alice"
