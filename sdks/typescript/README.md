# clarive-sdk

The official TypeScript SDK for the [Clarive](https://clarive.com) Public API. Fetch prompt entries, render templates with variable substitution, and let the built-in retry and circuit breaker handle the rest.

**Node 18+**, strict TypeScript, zero runtime dependencies. Ships ESM and CJS.

## Install

```
npm install clarive-sdk
```

## Quick start

```typescript
import { ClariveClient } from "clarive-sdk";

const client = new ClariveClient({
  apiKey: "cl_your_api_key",
});

// Fetch a published prompt entry
const entry = await client.getEntry("3fa85f64-5717-4562-b3fc-2c963f66afa6");

// Render it with template variables
const result = await client.generate(entry.id, {
  fields: {
    companyName: "Acme Corp",
    customerMessage: "I need help with my order",
  },
});

for (const prompt of result.renderedPrompts) {
  console.log(prompt.content);
}
```

## Resilience

Retry, circuit breaker, and timeout are on by default. The defaults:

| Setting | Default |
|---------|---------|
| Max retries | 3 |
| Retry base delay | 1 second (exponential backoff + jitter) |
| Timeout | 30 seconds |
| Circuit breaker threshold | 5 consecutive failures |
| Circuit breaker cooldown | 30 seconds |

Only transient errors get retried: timeouts, 429 (rate limited), and 5xx responses. Permanent errors like 401, 404, and 422 throw immediately.

Override the defaults:

```typescript
const client = new ClariveClient({
  apiKey: "cl_your_api_key",
  resilience: { maxRetries: 5, timeout: 60 },
});
```

Or turn it all off:

```typescript
const client = new ClariveClient({
  apiKey: "cl_your_api_key",
  resilience: { enabled: false },
});
```

## Error handling

API errors throw typed classes. All API exceptions extend `ClariveApiError`, which extends `ClariveError`:

| Error class | HTTP status | When |
|-------------|-------------|------|
| `ClariveAuthenticationError` | 401 | Invalid or missing API key |
| `ClariveNotFoundError` | 404 | Entry doesn't exist or is trashed |
| `ClariveValidationError` | 422 | Bad template field values |
| `ClariveRateLimitError` | 429 | Too many requests (limit: 20/min) |

`ClariveCircuitOpenError` extends `ClariveError` directly, not `ClariveApiError`. Catching `ClariveApiError` won't accidentally swallow circuit breaker errors.

```typescript
import {
  ClariveApiError,
  ClariveValidationError,
  ClariveCircuitOpenError,
} from "clarive-sdk";

try {
  const result = await client.generate(entryId, request);
} catch (e) {
  if (e instanceof ClariveValidationError) {
    for (const [field, error] of Object.entries(e.details)) {
      console.log(`${field}: ${error}`);
    }
  } else if (e instanceof ClariveApiError) {
    console.log(`API error ${e.errorCode}: ${e.message}`);
  } else if (e instanceof ClariveCircuitOpenError) {
    console.log("Service unavailable — circuit breaker is open");
  }
}
```

## API reference

### `ClariveClient`

| Method | Returns | Description |
|--------|---------|-------------|
| `getEntry(entryId: string)` | `Promise<PromptEntry>` | Fetches the published version of a prompt entry |
| `generate(entryId: string, request: GenerateRequest)` | `Promise<GenerateResponse>` | Renders prompts with template variable substitution |

### Models

All models are TypeScript interfaces with full IntelliSense support.

**`PromptEntry`** — a published prompt entry.

- `id` (string), `title` (string), `version` (number), `systemMessage` (string | null), `prompts` (Prompt[])

**`Prompt`** — a single prompt within an entry.

- `content` (string), `order` (number), `isTemplate` (boolean), `templateFields` (TemplateField[] | null)

**`TemplateField`** — a `{{variable}}` placeholder definition.

- `name`, `type` (string/int/float/enum), `enumValues`, `defaultValue`, `min`, `max`

**`GenerateRequest`** — request body for `generate()`.

- `fields?` (Record<string, string>) — variable names to values

**`GenerateResponse`** — rendered output.

- `id`, `title`, `version`, `systemMessage`, `renderedPrompts` (RenderedPrompt[])

**`RenderedPrompt`** — a prompt with variables replaced.

- `content` (string), `order` (number)

## Configuration

`ClariveOptions` controls SDK behavior:

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `apiKey` | string | (required) | Your API key (starts with `cl_`) |
| `baseUrl` | string | `https://app.clarive.com` | Clarive instance URL |
| `allowInsecureHttp` | boolean | `false` | Permit HTTP URLs (local dev only) |
| `resilience` | `ResilienceOptions` | Enabled | Retry, circuit breaker, and timeout settings |

HTTPS is enforced by default. For local development against `http://localhost`, set `allowInsecureHttp: true`.

The client's `toJSON()` method omits the API key, so `JSON.stringify(client)` won't leak credentials.

## Build and test

```bash
cd sdks/typescript
pnpm install
pnpm run build        # tsup → dist/ (ESM + CJS + .d.ts)
pnpm test             # vitest
pnpm run lint         # biome check
pnpm run check        # biome + tsc --noEmit
```

## License

See the [repository root](../../) for license information.
