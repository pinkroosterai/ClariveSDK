# clarive-sdk

[![PyPI](https://img.shields.io/pypi/v/clarive-sdk)](https://pypi.org/project/clarive-sdk/)

The official Python SDK for the [Clarive](https://clarive.com) Public API. Fetch prompt entries, render templates with variable substitution, and let the built-in retry and circuit breaker handle the rest.

Requires **Python 3.10+**. Two runtime dependencies: `httpx` and `tenacity`.

## Install

```
pip install clarive-sdk
```

## Quick start

Both async and sync clients are included. Pick whichever fits your project.

**Async:**

```python
from uuid import UUID
from clarive import ClariveClient, GenerateRequest

async with ClariveClient(api_key="cl_your_api_key") as client:
    # Fetch a published prompt entry
    entry = await client.get_entry(UUID("3fa85f64-5717-4562-b3fc-2c963f66afa6"))

    # Render it with template variables
    result = await client.generate(entry.id, GenerateRequest(
        fields={"companyName": "Acme Corp", "customerMessage": "I need help with my order"}
    ))

    for prompt in result.rendered_prompts:
        print(prompt.content)
```

**Sync:**

```python
from clarive import ClariveClientSync, GenerateRequest

with ClariveClientSync(api_key="cl_your_api_key") as client:
    entry = client.get_entry(entry_id)
    result = client.generate(entry.id, GenerateRequest(fields={"name": "Alice"}))
```

Both clients support manual lifecycle management too — call `await client.aclose()` or `client.close()` instead of using context managers.

## Resilience

Retry, circuit breaker, and timeout are enabled by default. The defaults:

| Setting | Default |
|---------|---------|
| Max retries | 3 |
| Retry base delay | 1 second (exponential backoff + jitter) |
| Timeout | 30 seconds |
| Circuit breaker threshold | 5 consecutive failures |
| Circuit breaker cooldown | 30 seconds |

Only transient errors get retried: timeouts, 429 (rate limited), and 5xx responses. Permanent errors like 401, 404, and 422 raise immediately.

Override the defaults:

```python
from clarive import ClariveClient, ClariveOptions, ResilienceOptions

client = ClariveClient(options=ClariveOptions(
    api_key="cl_your_api_key",
    resilience=ResilienceOptions(max_retries=5, timeout=60.0),
))
```

Or turn it all off:

```python
client = ClariveClient(options=ClariveOptions(
    api_key="cl_your_api_key",
    resilience=ResilienceOptions(enabled=False),
))
```

## Error handling

API errors raise typed exceptions. All API exceptions extend `ClariveApiError`, which extends `ClariveError`:

| Exception | HTTP status | When |
|-----------|-------------|------|
| `ClariveAuthenticationError` | 401 | Invalid or missing API key |
| `ClariveNotFoundError` | 404 | Entry doesn't exist or is trashed |
| `ClariveValidationError` | 422 | Bad template field values |
| `ClariveRateLimitError` | 429 | Too many requests (limit: 20/min) |

`ClariveCircuitOpenError` extends `ClariveError` directly — not `ClariveApiError` — so catching `ClariveApiError` won't accidentally swallow circuit breaker errors.

```python
from clarive import ClariveApiError, ClariveValidationError, ClariveCircuitOpenError

try:
    result = await client.generate(entry_id, request)
except ClariveValidationError as e:
    for field, error in e.details.items():
        print(f"{field}: {error}")
except ClariveRateLimitError:
    # Back off and retry
    pass
except ClariveApiError as e:
    print(f"API error {e.error_code}: {e}")
except ClariveCircuitOpenError:
    print("Service unavailable — circuit breaker is open")
```

## API reference

### `ClariveClient` / `ClariveClientSync`

| Method | Returns | Description |
|--------|---------|-------------|
| `get_entry(entry_id: UUID)` | `PromptEntry` | Fetches the published version of a prompt entry |
| `generate(entry_id: UUID, request: GenerateRequest)` | `GenerateResponse` | Renders prompts with template variable substitution |
| `list_entries(options?: ListEntriesOptions)` | `PaginatedResponse` | Lists published entries with filtering, search, and pagination |
| `list_tags()` | `list[TagInfo]` | Lists all tags with entry counts |

### Models

All response models are frozen dataclasses. They're immutable once created.

**`PromptEntry`** — a published prompt entry.

- `id` (UUID), `title` (str), `version` (int), `system_message` (str | None), `prompts` (list[Prompt]), `tags` (list[str]), `updated_at` (datetime), `published_at` (datetime | None)

**`Prompt`** — a single prompt within an entry.

- `content` (str), `order` (int), `is_template` (bool), `template_fields` (list[TemplateField] | None)

**`TemplateField`** — a `{{variable}}` placeholder definition.

- `id` (UUID), `prompt_id` (UUID), `name`, `type` (string/int/float/enum), `enum_values`, `default_value`, `min`, `max`

**`GenerateRequest`** — request body for `generate()`.

- `fields` (dict[str, str] | None) — variable names to values

**`GenerateResponse`** — rendered output.

- `id`, `title`, `version`, `system_message`, `rendered_prompts` (list[RenderedPrompt])

**`RenderedPrompt`** — a prompt with variables replaced.

- `content` (str), `order` (int)

**`EntrySummary`** — compact entry representation from `list_entries()`.

- `id`, `title`, `version`, `has_system_message`, `is_template`, `is_chain`, `prompt_count`, `first_prompt_preview`, `tags`, `created_at`, `updated_at`

**`PaginatedResponse`** — pagination wrapper.

- `items` (list), `total_count` (int), `page` (int), `page_size` (int)

**`TagInfo`** — a tag with its entry count.

- `name` (str), `entry_count` (int)

**`ListEntriesOptions`** — query parameters for `list_entries()`.

- `folder_id`, `tags`, `tag_mode`, `page`, `page_size`, `search`, `sort_by` (all optional)

## Configuration

`ClariveOptions` controls SDK behavior:

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `api_key` | str | `""` | Your API key (required, starts with `cl_`) |
| `base_url` | str | `https://app.clarive.com` | Clarive instance URL |
| `allow_insecure_http` | bool | `False` | Permit HTTP URLs (local dev only) |
| `resilience` | `ResilienceOptions` | Enabled | Retry, circuit breaker, and timeout settings |

HTTPS is enforced by default. For local development against `http://localhost`, set `allow_insecure_http=True`.

The `api_key` field is excluded from `repr()` output, so printing or logging a `ClariveOptions` object won't leak your key.

## Build and test

```bash
cd sdks/python
uv venv && uv pip install -e ".[dev]"
uv run pytest
uv run ruff check src/ tests/
uv run mypy --strict src/
```

## License

See the [repository root](../../) for license information.
