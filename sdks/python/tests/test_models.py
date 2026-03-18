from uuid import UUID

from clarive.models import (
    GenerateRequest,
    GenerateResponse,
    PromptEntry,
)

ENTRY_JSON = {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "title": "Customer Support Reply",
    "systemMessage": "You are a helpful customer support agent for {{companyName}}.",
    "version": 3,
    "prompts": [
        {
            "content": "Draft a reply to the following customer inquiry:\n\n{{customerMessage}}",
            "order": 1,
            "isTemplate": True,
            "templateFields": [
                {
                    "id": "00000000-0000-0000-0000-000000000001",
                    "promptId": "00000000-0000-0000-0000-000000000002",
                    "name": "customerMessage",
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

GENERATE_RESPONSE_JSON = {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "title": "Customer Support Reply",
    "version": 3,
    "systemMessage": "You are a helpful customer support agent for Acme Corp.",
    "renderedPrompts": [
        {
            "content": (
                "Draft a reply to the following customer inquiry:"
                "\n\nI need help with my order #12345"
            ),
            "order": 1,
        }
    ],
}


class TestPromptEntryFromDict:
    def test_deserializes_full_entry(self) -> None:
        entry = PromptEntry.from_dict(ENTRY_JSON)

        assert entry.id == UUID("3fa85f64-5717-4562-b3fc-2c963f66afa6")
        assert entry.title == "Customer Support Reply"
        expected_msg = "You are a helpful customer support agent for {{companyName}}."
        assert entry.system_message == expected_msg
        assert entry.version == 3
        assert len(entry.prompts) == 1
        assert entry.tags == ["test"]
        assert entry.updated_at is not None
        assert entry.published_at is not None

    def test_deserializes_prompt_with_template_fields(self) -> None:
        entry = PromptEntry.from_dict(ENTRY_JSON)
        prompt = entry.prompts[0]

        assert prompt.order == 1
        assert prompt.is_template is True
        assert prompt.template_fields is not None
        assert len(prompt.template_fields) == 1

        field = prompt.template_fields[0]
        assert field.id == UUID("00000000-0000-0000-0000-000000000001")
        assert field.prompt_id == UUID("00000000-0000-0000-0000-000000000002")
        assert field.name == "customerMessage"
        assert field.type == "string"
        assert field.enum_values is None
        assert field.default_value is None
        assert field.min is None
        assert field.max is None

    def test_null_system_message(self) -> None:
        data = {**ENTRY_JSON, "systemMessage": None}
        entry = PromptEntry.from_dict(data)
        assert entry.system_message is None

    def test_null_template_fields(self) -> None:
        data = {
            **ENTRY_JSON,
            "prompts": [
                {
                    "content": "Static prompt",
                    "order": 1,
                    "isTemplate": False,
                    "templateFields": None,
                }
            ],
        }
        entry = PromptEntry.from_dict(data)
        assert entry.prompts[0].template_fields is None
        assert entry.prompts[0].is_template is False

    def test_empty_prompts_list(self) -> None:
        data = {**ENTRY_JSON, "prompts": []}
        entry = PromptEntry.from_dict(data)
        assert entry.prompts == []

    def test_template_field_with_enum_values(self) -> None:
        data = {
            **ENTRY_JSON,
            "prompts": [
                {
                    "content": "Pick a color: {{color}}",
                    "order": 1,
                    "isTemplate": True,
                    "templateFields": [
                        {
                            "id": "00000000-0000-0000-0000-000000000001",
                            "promptId": "00000000-0000-0000-0000-000000000002",
                            "name": "color",
                            "type": "enum",
                            "enumValues": ["red", "green", "blue"],
                            "defaultValue": "red",
                            "min": None,
                            "max": None,
                        }
                    ],
                }
            ],
        }
        entry = PromptEntry.from_dict(data)
        field = entry.prompts[0].template_fields[0]  # type: ignore[index]
        assert field.enum_values == ["red", "green", "blue"]
        assert field.default_value == "red"

    def test_template_field_with_min_max(self) -> None:
        data = {
            **ENTRY_JSON,
            "prompts": [
                {
                    "content": "Temperature: {{temp}}",
                    "order": 1,
                    "isTemplate": True,
                    "templateFields": [
                        {
                            "id": "00000000-0000-0000-0000-000000000001",
                            "promptId": "00000000-0000-0000-0000-000000000002",
                            "name": "temp",
                            "type": "float",
                            "enumValues": None,
                            "defaultValue": None,
                            "min": 0.0,
                            "max": 1.0,
                        }
                    ],
                }
            ],
        }
        entry = PromptEntry.from_dict(data)
        field = entry.prompts[0].template_fields[0]  # type: ignore[index]
        assert field.min == 0.0
        assert field.max == 1.0

    def test_frozen_immutability(self) -> None:
        import pytest

        entry = PromptEntry.from_dict(ENTRY_JSON)
        with pytest.raises(AttributeError):
            entry.title = "Modified"  # type: ignore[misc]


class TestGenerateResponseFromDict:
    def test_deserializes_full_response(self) -> None:
        resp = GenerateResponse.from_dict(GENERATE_RESPONSE_JSON)

        assert resp.id == UUID("3fa85f64-5717-4562-b3fc-2c963f66afa6")
        assert resp.title == "Customer Support Reply"
        assert resp.version == 3
        assert resp.system_message == "You are a helpful customer support agent for Acme Corp."
        assert len(resp.rendered_prompts) == 1

    def test_rendered_prompt_content(self) -> None:
        resp = GenerateResponse.from_dict(GENERATE_RESPONSE_JSON)
        rp = resp.rendered_prompts[0]
        assert rp.order == 1
        assert "I need help with my order #12345" in rp.content

    def test_null_system_message(self) -> None:
        data = {**GENERATE_RESPONSE_JSON, "systemMessage": None}
        resp = GenerateResponse.from_dict(data)
        assert resp.system_message is None


class TestGenerateRequest:
    def test_to_dict_with_fields(self) -> None:
        req = GenerateRequest(fields={"name": "Alice"})
        assert req.to_dict() == {"fields": {"name": "Alice"}}

    def test_to_dict_without_fields(self) -> None:
        req = GenerateRequest()
        assert req.to_dict() == {"fields": None}
