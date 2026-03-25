# Clarive Public API Reference

## Overview

The Clarive Public API provides programmatic access to published prompt entries. It allows you to list, retrieve, and render prompt entries with template variable substitution.

- **Base URL**: `https://<your-instance>.clarive.app/public/v1`
- **Protocol**: HTTPS
- **Response Format**: JSON (camelCase property names)
- **Authentication**: API Key via `X-Api-Key` header
- **Rate Limiting**: 600 requests per minute per API key (fixed window)

---

## Authentication

All requests require an API key passed in the `X-Api-Key` header.

### API Key Format

- Prefix: `cl_`
- Total length: ~38 characters
- Example: `cl_a1b2c3d4e5f6...`

API keys are created through the Clarive admin UI and are scoped to a single tenant. Each key can only access entries belonging to its tenant.

### Header

```
X-Api-Key: cl_your_api_key_here
```

> **Note**: The full key is shown only once at creation time. Clarive stores a SHA256 hash internally.

---

## Endpoints

### List Published Entries

Lists published prompt entries with optional filtering, search, and pagination.

```
GET /public/v1/entries
```

#### Query Parameters

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `folderId` | string | No | all | Folder ID (GUID) to filter by, or `"all"` for all folders |
| `tags` | string | No | — | Comma-separated tag names to filter by |
| `tagMode` | string | No | `"or"` | Tag filter mode: `"and"` (all tags) or `"or"` (any tag) |
| `page` | integer | No | 1 | Page number (1-based) |
| `pageSize` | integer | No | 50 | Items per page (max 100) |
| `search` | string | No | — | Search by title (case-insensitive) |
| `sortBy` | string | No | `"recent"` | Sort order: `"recent"`, `"alphabetical"`, or `"oldest"` |

#### Response `200 OK`

```json
{
  "items": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "title": "Customer Support Reply",
      "version": 3,
      "hasSystemMessage": true,
      "isTemplate": true,
      "isChain": false,
      "promptCount": 1,
      "firstPromptPreview": "Draft a reply to the following customer inquiry...",
      "tags": ["support", "customer"],
      "createdAt": "2026-03-15T10:00:00Z",
      "updatedAt": "2026-03-18T10:00:00Z",
      "tabs": [
        { "id": "a1b2c3d4-0000-0000-0000-000000000001", "name": "Main", "isMainTab": true, "forkedFromVersion": null }
      ],
      "tabCount": 1
    }
  ],
  "totalCount": 42,
  "page": 1,
  "pageSize": 50
}
```

#### Entry Summary Fields

| Field | Type | Description |
|-------|------|-------------|
| `id` | string (GUID) | Entry unique identifier |
| `title` | string | Display name of the entry |
| `version` | integer | Published version number |
| `hasSystemMessage` | boolean | Whether the entry has a system message |
| `isTemplate` | boolean | Whether the entry contains template variables |
| `isChain` | boolean | Whether the entry has multiple prompts |
| `promptCount` | integer | Number of prompts in the entry |
| `firstPromptPreview` | string \| null | Preview text from the first prompt |
| `tags` | string[] | Tags assigned to the entry |
| `createdAt` | string (ISO 8601) | When the entry was created |
| `updatedAt` | string (ISO 8601) | When the entry was last updated |
| `tabs` | TabSummary[] | Tab summaries for this entry |
| `tabCount` | integer | Number of tabs on this entry |

#### Pagination Wrapper

| Field | Type | Description |
|-------|------|-------------|
| `items` | array | Items on the current page |
| `totalCount` | integer | Total number of items across all pages |
| `page` | integer | Current page number (1-based) |
| `pageSize` | integer | Number of items per page |

---

### Get Published Prompt Entry

Retrieves the currently published version of a prompt entry, including its prompts and template field definitions.

```
GET /public/v1/entries/{entryId}
```

#### Path Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `entryId` | GUID | Yes | Unique identifier of the prompt entry |

#### Response `200 OK`

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "title": "Customer Support Reply",
  "systemMessage": "You are a helpful customer support agent for {{companyName}}.",
  "version": 3,
  "prompts": [
    {
      "content": "Draft a reply to the following customer inquiry:\n\n{{customerMessage}}",
      "order": 1,
      "isTemplate": true,
      "templateFields": [
        {
          "id": "019ce71e-651d-7003-981e-f7d916f1bcdb",
          "promptId": "7d38ca52-ad3a-4fda-8def-58b97a590937",
          "name": "customerMessage",
          "type": "string",
          "enumValues": null,
          "defaultValue": null,
          "min": null,
          "max": null
        }
      ]
    }
  ],
  "tags": ["support", "customer"],
  "updatedAt": "2026-03-18T10:00:00Z",
  "publishedAt": "2026-03-17T14:30:00Z",
  "tabs": [
    { "id": "a1b2c3d4-0000-0000-0000-000000000001", "name": "Main", "isMainTab": true, "forkedFromVersion": null }
  ],
  "tabCount": 1
}
```

#### Response Fields

| Field | Type | Description |
|-------|------|-------------|
| `id` | string (GUID) | Entry unique identifier |
| `title` | string | Display name of the entry |
| `systemMessage` | string \| null | Optional system message (may contain `{{variable}}` placeholders) |
| `version` | integer | Published version number |
| `prompts` | array | Ordered list of prompts |
| `prompts[].content` | string | Prompt text (may contain `{{variable}}` placeholders) |
| `prompts[].order` | integer | Display/execution order |
| `prompts[].isTemplate` | boolean | Whether this prompt contains template variables |
| `prompts[].templateFields` | array \| null | Template variable definitions (present only when `isTemplate` is `true`) |
| `tags` | string[] | Tags assigned to the entry |
| `updatedAt` | string (ISO 8601) | When the entry was last updated |
| `publishedAt` | string (ISO 8601) \| null | When the current version was published |
| `tabs` | TabSummary[] | Tab summaries for this entry |
| `tabCount` | integer | Number of tabs on this entry |

#### Tab Summary Object

| Field | Type | Description |
|-------|------|-------------|
| `id` | string (GUID) | Unique identifier of the tab |
| `name` | string | Display name of the tab |
| `isMainTab` | boolean | Whether this is the main (default) tab |
| `forkedFromVersion` | integer \| null | Version number this tab was forked from, if any |

#### Template Field Object

| Field | Type | Description |
|-------|------|-------------|
| `id` | string (GUID) | Unique identifier of the template field |
| `promptId` | string (GUID) | Identifier of the prompt this field belongs to |
| `name` | string | Variable name (matches `{{name}}` in content) |
| `type` | string | One of: `string`, `int`, `float`, `enum` |
| `enumValues` | string[] \| null | Allowed values (only for `enum` type) |
| `defaultValue` | string \| null | Default value if not provided |
| `min` | number \| null | Minimum value (only for `int` and `float` types) |
| `max` | number \| null | Maximum value (only for `int` and `float` types) |

---

### Generate Rendered Prompt Entry

Renders a published prompt entry by substituting template variables with the provided values.

```
POST /public/v1/entries/{entryId}/generate
```

#### Path Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `entryId` | GUID | Yes | Unique identifier of the prompt entry |

#### Request Body

```json
{
  "fields": {
    "companyName": "Acme Corp",
    "customerMessage": "I need help with my order #12345"
  }
}
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `fields` | object | No | Key-value pairs mapping template variable names to their values. All values are strings. Max 10,000 characters per value. |

#### Response `200 OK`

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "title": "Customer Support Reply",
  "version": 3,
  "systemMessage": "You are a helpful customer support agent for Acme Corp.",
  "renderedPrompts": [
    {
      "content": "Draft a reply to the following customer inquiry:\n\nI need help with my order #12345",
      "order": 1
    }
  ]
}
```

#### Response Fields

| Field | Type | Description |
|-------|------|-------------|
| `id` | string (GUID) | Entry unique identifier |
| `title` | string | Display name of the entry |
| `version` | integer | Published version number |
| `systemMessage` | string \| null | System message with variables rendered |
| `renderedPrompts` | array | Prompts with all template variables substituted |
| `renderedPrompts[].content` | string | Fully rendered prompt text |
| `renderedPrompts[].order` | integer | Display/execution order |

#### Field Validation Rules

| Type | Rules |
|------|-------|
| `string` | Required (non-empty). Max 10,000 characters. |
| `int` | Must parse as a 32-bit integer. Optional `min`/`max` constraints. |
| `float` | Must parse as a double. Optional `min`/`max` constraints. |
| `enum` | Must match one of the defined `enumValues` (case-insensitive). |

---

### List Tabs

Lists tabs for a prompt entry. Response is cached for 5 minutes.

```
GET /public/v1/entries/{entryId}/tabs
```

#### Path Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `entryId` | GUID | Yes | Unique identifier of the prompt entry |

#### Response `200 OK`

```json
[
  { "id": "a1b2c3d4-0000-0000-0000-000000000001", "name": "Main", "isMainTab": true, "forkedFromVersion": null },
  { "id": "b2c3d4e5-0000-0000-0000-000000000002", "name": "Formal Tone", "isMainTab": false, "forkedFromVersion": 3 }
]
```

---

### Get Tab

Retrieves a specific tab of a prompt entry. Returns the same shape as Get Published Prompt Entry.

```
GET /public/v1/entries/{entryId}/tabs/{tabId}
```

#### Path Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `entryId` | GUID | Yes | Unique identifier of the prompt entry |
| `tabId` | GUID | Yes | Unique identifier of the tab |

#### Response `200 OK`

Same shape as [Get Published Prompt Entry](#get-published-prompt-entry).

---

### Generate from Tab

Renders a tab by substituting template variables. Same request/response shape as Generate Rendered Prompt Entry.

```
POST /public/v1/entries/{entryId}/tabs/{tabId}/generate
```

#### Path Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `entryId` | GUID | Yes | Unique identifier of the prompt entry |
| `tabId` | GUID | Yes | Unique identifier of the tab |

#### Request Body

Same shape as [Generate Rendered Prompt Entry](#generate-rendered-prompt-entry).

#### Response `200 OK`

Same shape as [Generate Rendered Prompt Entry](#generate-rendered-prompt-entry).

---

### List Tags

Lists all tags with their entry counts.

```
GET /public/v1/tags
```

#### Response `200 OK`

```json
[
  { "name": "support", "entryCount": 12 },
  { "name": "onboarding", "entryCount": 5 }
]
```

#### Response Fields

| Field | Type | Description |
|-------|------|-------------|
| `name` | string | The tag name |
| `entryCount` | integer | Number of entries with this tag |

---

## Template Variable Syntax

Template variables in prompt content use the `{{variableName}}` syntax. The parser supports type annotations and constraints:

| Syntax | Description | Example |
|--------|-------------|---------|
| `{{name}}` | String variable | `{{userName}}` |
| `{{name\|int}}` | Integer variable | `{{age\|int}}` |
| `{{name\|float:min-max}}` | Float with range | `{{temperature\|float:0-1}}` |
| `{{name\|int:min-max}}` | Integer with range | `{{count\|int:1-10}}` |
| `{{name\|enum:a,b,c}}` | Enum with allowed values | `{{color\|enum:red,green,blue}}` |

---

## Error Handling

All errors follow a consistent format:

```json
{
  "error": {
    "code": "ERROR_CODE",
    "message": "Human-readable description",
    "details": {}
  }
}
```

The `details` field is optional and only present for validation errors.

### Error Codes

| HTTP Status | Code | Description |
|-------------|------|-------------|
| 401 | `UNAUTHORIZED` | Invalid or missing API key |
| 404 | `ENTRY_NOT_FOUND` | Entry does not exist or is trashed |
| 404 | `NO_PUBLISHED_VERSION` | Entry exists but has no published version |
| 404 | `TAB_NOT_FOUND` | Tab does not exist for this entry |
| 422 | `VALIDATION_ERROR` | Template field validation failed (see `details` for per-field errors) |
| 429 | `RATE_LIMITED` | Rate limit exceeded (600 req/min) |
| 500 | `INTERNAL_ERROR` | Unexpected server error |

### Validation Error Example

```json
{
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "Template field validation failed.",
    "details": {
      "age": "Field 'age' must be a valid integer.",
      "color": "Field 'color' must be one of: red, green, blue."
    }
  }
}
```

---

## Rate Limiting

| Property | Value |
|----------|-------|
| Limit | 600 requests per minute |
| Window | Fixed 1-minute window |
| Partition | API key (falls back to client IP if unauthenticated) |
| Exceeded response | `429 Too Many Requests` with `Retry-After` header |

### Rate Limit Response Headers

All `/public/v1/` responses include rate limit headers:

| Header | Description |
|--------|-------------|
| `X-RateLimit-Limit` | Maximum requests allowed per window |
| `X-RateLimit-Remaining` | Remaining requests in the current window |
| `X-RateLimit-Reset` | Unix timestamp when the window resets |

---

## Multi-Tenant Isolation

API keys are scoped to a single tenant. An API key can only access entries that belong to its tenant. There is no way to query or access entries from another tenant.

---

## Audit Logging

All public API calls are logged with:

- Tenant ID
- API key ID and name
- Action type (`ApiGet`, `ApiGenerate`, `ApiList`, `ApiGetTab`, or `ApiGenerateTab`)
- Entry ID and title (where applicable)
- Timestamp

---

## Examples

### cURL

**List entries:**

```bash
curl -X GET "https://demo.clarive.app/public/v1/entries?page=1&pageSize=10&search=support" \
  -H "X-Api-Key: cl_your_api_key_here"
```

**Get an entry:**

```bash
curl -X GET "https://demo.clarive.app/public/v1/entries/3fa85f64-5717-4562-b3fc-2c963f66afa6" \
  -H "X-Api-Key: cl_your_api_key_here"
```

**Generate a rendered prompt:**

```bash
curl -X POST "https://demo.clarive.app/public/v1/entries/3fa85f64-5717-4562-b3fc-2c963f66afa6/generate" \
  -H "X-Api-Key: cl_your_api_key_here" \
  -H "Content-Type: application/json" \
  -d '{
    "fields": {
      "userName": "Alice",
      "topic": "machine learning"
    }
  }'
```

**List tags:**

```bash
curl -X GET "https://demo.clarive.app/public/v1/tags" \
  -H "X-Api-Key: cl_your_api_key_here"
```

**List tabs for an entry:**

```bash
curl -X GET "https://demo.clarive.app/public/v1/entries/3fa85f64-5717-4562-b3fc-2c963f66afa6/tabs" \
  -H "X-Api-Key: cl_your_api_key_here"
```

**Get a specific tab:**

```bash
curl -X GET "https://demo.clarive.app/public/v1/entries/3fa85f64-5717-4562-b3fc-2c963f66afa6/tabs/a1b2c3d4-0000-0000-0000-000000000001" \
  -H "X-Api-Key: cl_your_api_key_here"
```

**Generate from a tab:**

```bash
curl -X POST "https://demo.clarive.app/public/v1/entries/3fa85f64-5717-4562-b3fc-2c963f66afa6/tabs/a1b2c3d4-0000-0000-0000-000000000001/generate" \
  -H "X-Api-Key: cl_your_api_key_here" \
  -H "Content-Type: application/json" \
  -d '{
    "fields": {
      "userName": "Alice",
      "topic": "machine learning"
    }
  }'
```

### Python

```python
from clarive import ClariveClient, ListEntriesOptions, GenerateRequest

async with ClariveClient(api_key="cl_your_api_key_here", base_url="https://demo.clarive.app") as client:
    # List entries
    entries = await client.list_entries(ListEntriesOptions(search="support", page=1))
    for entry in entries.items:
        print(f"{entry.title} (v{entry.version})")

    # Get entry
    entry = await client.get_entry(entries.items[0].id)

    # Generate
    result = await client.generate(entry.id, GenerateRequest(fields={"userName": "Alice"}))

    # List tags
    tags = await client.list_tags()

    # List tabs
    tabs = await client.list_tabs(entry.id)

    # Get a tab
    tab = await client.get_tab(entry.id, tabs[0].id)

    # Generate from a tab
    tab_result = await client.generate_tab(entry.id, tabs[0].id, GenerateRequest(fields={"userName": "Alice"}))
```

### TypeScript

```typescript
import { ClariveClient } from "clarive-sdk";

const client = new ClariveClient({ apiKey: "cl_your_api_key_here", baseUrl: "https://demo.clarive.app" });

// List entries
const entries = await client.listEntries({ search: "support", page: 1 });
for (const entry of entries.items) {
  console.log(`${entry.title} (v${entry.version})`);
}

// Get entry
const entry = await client.getEntry(entries.items[0].id);

// Generate
const result = await client.generate(entry.id, { fields: { userName: "Alice" } });

// List tags
const tags = await client.listTags();

// List tabs
const tabs = await client.listTabs(entry.id);

// Get a tab
const tab = await client.getTab(entry.id, tabs[0].id);

// Generate from a tab
const tabResult = await client.generateTab(entry.id, tabs[0].id, { fields: { userName: "Alice" } });
```

### C#

```csharp
using var httpClient = new HttpClient();
var client = new ClariveClient(httpClient, new ClariveOptions
{
    ApiKey = "cl_your_api_key_here",
    BaseUrl = "https://demo.clarive.app"
});

// List entries
var entries = await client.ListEntriesAsync(new ListEntriesOptions { Search = "support", Page = 1 });
foreach (var summary in entries.Items)
    Console.WriteLine($"{summary.Title} (v{summary.Version})");

// Get entry
var entry = await client.GetEntryAsync(entries.Items[0].Id);

// Generate
var result = await client.GenerateAsync(entry.Id, new GenerateRequest
{
    Fields = new Dictionary<string, string> { ["userName"] = "Alice" }
});

// List tags
var tags = await client.ListTagsAsync();

// List tabs
var tabs = await client.ListTabsAsync(entry.Id);

// Get a tab
var tab = await client.GetTabAsync(entry.Id, tabs[0].Id);

// Generate from a tab
var tabResult = await client.GenerateTabAsync(entry.Id, tabs[0].Id, new GenerateRequest
{
    Fields = new Dictionary<string, string> { ["userName"] = "Alice" }
});
```
