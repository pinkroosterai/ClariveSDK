# Clarive Public API Reference

## Overview

The Clarive Public API provides programmatic access to published prompt entries. It allows you to retrieve prompt definitions and generate rendered prompts with template variable substitution.

- **Base URL**: `https://<your-instance>.clarive.app/public/v1`
- **Protocol**: HTTPS
- **Response Format**: JSON (camelCase property names)
- **Authentication**: API Key via `X-Api-Key` header
- **Rate Limiting**: 20 requests per minute per IP (fixed window)

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
          "name": "customerMessage",
          "type": "string",
          "enumValues": null,
          "defaultValue": null,
          "min": null,
          "max": null
        }
      ]
    }
  ]
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

#### Template Field Object

| Field | Type | Description |
|-------|------|-------------|
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
| 404 | `NOT_FOUND` | Entry does not exist, is trashed, or has no published version |
| 422 | `VALIDATION_ERROR` | Template field validation failed (see `details` for per-field errors) |
| 429 | `RATE_LIMITED` | Rate limit exceeded (20 req/min) |
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
| Limit | 20 requests per minute |
| Window | Fixed 1-minute window |
| Partition | Client IP address |
| Exceeded response | `429 Too Many Requests` |

---

## Multi-Tenant Isolation

API keys are scoped to a single tenant. An API key can only access entries that belong to its tenant. There is no way to query or access entries from another tenant.

---

## Audit Logging

All public API calls are logged with:

- Tenant ID
- API key ID and name
- Action type (`ApiGet` or `ApiGenerate`)
- Entry ID and title
- Timestamp

---

## Examples

### cURL

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

### Python

```python
import requests

BASE_URL = "https://demo.clarive.app/public/v1"
API_KEY = "cl_your_api_key_here"
ENTRY_ID = "3fa85f64-5717-4562-b3fc-2c963f66afa6"

headers = {"X-Api-Key": API_KEY}

# Get entry
response = requests.get(f"{BASE_URL}/entries/{ENTRY_ID}", headers=headers)
entry = response.json()

# Generate
response = requests.post(
    f"{BASE_URL}/entries/{ENTRY_ID}/generate",
    headers=headers,
    json={"fields": {"userName": "Alice"}},
)
result = response.json()
```

### TypeScript

```typescript
const BASE_URL = "https://demo.clarive.app/public/v1";
const API_KEY = "cl_your_api_key_here";
const ENTRY_ID = "3fa85f64-5717-4562-b3fc-2c963f66afa6";

// Get entry
const entry = await fetch(`${BASE_URL}/entries/${ENTRY_ID}`, {
  headers: { "X-Api-Key": API_KEY },
}).then((r) => r.json());

// Generate
const result = await fetch(`${BASE_URL}/entries/${ENTRY_ID}/generate`, {
  method: "POST",
  headers: { "X-Api-Key": API_KEY, "Content-Type": "application/json" },
  body: JSON.stringify({ fields: { userName: "Alice" } }),
}).then((r) => r.json());
```

### C#

```csharp
using var client = new HttpClient();
client.BaseAddress = new Uri("https://demo.clarive.app/public/v1/");
client.DefaultRequestHeaders.Add("X-Api-Key", "cl_your_api_key_here");

var entryId = "3fa85f64-5717-4562-b3fc-2c963f66afa6";

// Get entry
var entry = await client.GetFromJsonAsync<PublicPromptEntry>($"entries/{entryId}");

// Generate
var request = new { fields = new Dictionary<string, string> { ["userName"] = "Alice" } };
var response = await client.PostAsJsonAsync($"entries/{entryId}/generate", request);
var result = await response.Content.ReadFromJsonAsync<PublicGenerateResponse>();
```
