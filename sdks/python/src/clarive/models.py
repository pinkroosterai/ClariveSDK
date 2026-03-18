from __future__ import annotations

from dataclasses import dataclass
from datetime import datetime
from typing import Any
from uuid import UUID


@dataclass(frozen=True, slots=True)
class TemplateField:
    """Definition of a template variable placeholder in a prompt."""

    id: UUID
    prompt_id: UUID
    name: str
    type: str
    enum_values: list[str] | None
    default_value: str | None
    min: float | None
    max: float | None

    @classmethod
    def from_dict(cls, data: dict[str, Any]) -> TemplateField:
        return cls(
            id=UUID(str(data["id"])),
            prompt_id=UUID(str(data["promptId"])),
            name=str(data["name"]),
            type=str(data["type"]),
            enum_values=data.get("enumValues"),
            default_value=data.get("defaultValue"),
            min=data.get("min"),
            max=data.get("max"),
        )


@dataclass(frozen=True, slots=True)
class Prompt:
    """A single prompt within a prompt entry."""

    content: str
    order: int
    is_template: bool
    template_fields: list[TemplateField] | None

    @classmethod
    def from_dict(cls, data: dict[str, Any]) -> Prompt:
        raw_fields = data.get("templateFields")
        template_fields = (
            [TemplateField.from_dict(f) for f in raw_fields] if raw_fields is not None else None
        )
        return cls(
            content=str(data["content"]),
            order=int(data["order"]),
            is_template=bool(data["isTemplate"]),
            template_fields=template_fields,
        )


def _parse_datetime(value: Any) -> datetime:
    """Parse an ISO 8601 datetime string."""
    return datetime.fromisoformat(str(value))


def _parse_datetime_optional(value: Any) -> datetime | None:
    """Parse an optional ISO 8601 datetime string."""
    if value is None:
        return None
    return datetime.fromisoformat(str(value))


@dataclass(frozen=True, slots=True)
class PromptEntry:
    """A published prompt entry retrieved from the Clarive API."""

    id: UUID
    title: str
    system_message: str | None
    version: int
    prompts: list[Prompt]
    tags: list[str]
    updated_at: datetime
    published_at: datetime | None

    @classmethod
    def from_dict(cls, data: dict[str, Any]) -> PromptEntry:
        return cls(
            id=UUID(str(data["id"])),
            title=str(data["title"]),
            system_message=data.get("systemMessage"),
            version=int(data["version"]),
            prompts=[Prompt.from_dict(p) for p in data["prompts"]],
            tags=data.get("tags", []),
            updated_at=_parse_datetime(data["updatedAt"]),
            published_at=_parse_datetime_optional(data.get("publishedAt")),
        )


@dataclass(frozen=True, slots=True)
class RenderedPrompt:
    """A prompt with all template variables substituted."""

    content: str
    order: int

    @classmethod
    def from_dict(cls, data: dict[str, Any]) -> RenderedPrompt:
        return cls(
            content=str(data["content"]),
            order=int(data["order"]),
        )


@dataclass(frozen=True, slots=True)
class GenerateResponse:
    """Response from the generate endpoint with rendered prompts."""

    id: UUID
    title: str
    version: int
    system_message: str | None
    rendered_prompts: list[RenderedPrompt]

    @classmethod
    def from_dict(cls, data: dict[str, Any]) -> GenerateResponse:
        return cls(
            id=UUID(str(data["id"])),
            title=str(data["title"]),
            version=int(data["version"]),
            system_message=data.get("systemMessage"),
            rendered_prompts=[RenderedPrompt.from_dict(rp) for rp in data["renderedPrompts"]],
        )


@dataclass(slots=True)
class GenerateRequest:
    """Request body for the generate endpoint."""

    fields: dict[str, str] | None = None

    def to_dict(self) -> dict[str, Any]:
        return {"fields": self.fields}


@dataclass(frozen=True, slots=True)
class EntrySummary:
    """Compact representation of a published entry from the list endpoint."""

    id: UUID
    title: str
    version: int
    has_system_message: bool
    is_template: bool
    is_chain: bool
    prompt_count: int
    first_prompt_preview: str | None
    tags: list[str]
    created_at: datetime
    updated_at: datetime

    @classmethod
    def from_dict(cls, data: dict[str, Any]) -> EntrySummary:
        return cls(
            id=UUID(str(data["id"])),
            title=str(data["title"]),
            version=int(data["version"]),
            has_system_message=bool(data["hasSystemMessage"]),
            is_template=bool(data["isTemplate"]),
            is_chain=bool(data["isChain"]),
            prompt_count=int(data["promptCount"]),
            first_prompt_preview=data.get("firstPromptPreview"),
            tags=data.get("tags", []),
            created_at=_parse_datetime(data["createdAt"]),
            updated_at=_parse_datetime(data["updatedAt"]),
        )


@dataclass(frozen=True, slots=True)
class PaginatedResponse:
    """Paginated response wrapper from list endpoints."""

    items: list[Any]
    total_count: int
    page: int
    page_size: int

    @classmethod
    def from_dict(cls, data: dict[str, Any], item_factory: Any) -> PaginatedResponse:
        return cls(
            items=[item_factory(item) for item in data["items"]],
            total_count=int(data["totalCount"]),
            page=int(data["page"]),
            page_size=int(data["pageSize"]),
        )


@dataclass(frozen=True, slots=True)
class TagInfo:
    """A tag with its entry count, returned by the list tags endpoint."""

    name: str
    entry_count: int

    @classmethod
    def from_dict(cls, data: dict[str, Any]) -> TagInfo:
        return cls(
            name=str(data["name"]),
            entry_count=int(data["entryCount"]),
        )


@dataclass(slots=True)
class ListEntriesOptions:
    """Query parameters for the list entries endpoint."""

    folder_id: str | None = None
    tags: str | None = None
    tag_mode: str | None = None
    page: int | None = None
    page_size: int | None = None
    search: str | None = None
    sort_by: str | None = None
