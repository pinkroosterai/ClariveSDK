# ClariveSDK

Official SDKs for the [Clarive](https://clarive.com) Public API in C#, Python, and TypeScript.

Two endpoints, three languages, one consistent design: fetch prompt entries and render templates with variable substitution. Each SDK includes typed models, typed errors, and built-in retry with circuit breaker.

## SDKs

| Language | Package | Runtime | Deps |
|----------|---------|---------|------|
| [C#](sdks/csharp/) | `ClariveSDK` | .NET 9+ | Microsoft.Extensions.Http, Resilience |
| [Python](sdks/python/) | `clarive-sdk` | Python 3.10+ | httpx, tenacity |
| [TypeScript](sdks/typescript/) | `clarive-sdk` | Node 18+ | None (native fetch) |

## Quick start

**C#**
```csharp
var client = new ClariveClient(httpClient, new ClariveOptions { ApiKey = "cl_..." });
var entry = await client.GetEntryAsync(entryId);
```

**Python**
```python
async with ClariveClient(api_key="cl_...") as client:
    entry = await client.get_entry(entry_id)
```

**TypeScript**
```typescript
const client = new ClariveClient({ apiKey: "cl_..." });
const entry = await client.getEntry(entryId);
```

## Build and test

```bash
make build    # Build all three SDKs
make test     # Run all test suites
make clean    # Remove build artifacts
```

Or target a single SDK:

```bash
make build-csharp
make test-python
make test-typescript
```

## API

Both endpoints require an `X-Api-Key` header. Full spec in [CLARIVE_PUBLIC_API.md](CLARIVE_PUBLIC_API.md).

- **GET** `/public/v1/entries/{entryId}` — retrieve a published prompt entry
- **POST** `/public/v1/entries/{entryId}/generate` — render with template variables

## Design

Each SDK is idiomatic to its language. They share principles, not code:

- Single client class with API key and optional base URL
- Typed error hierarchy mapping HTTP status codes to specific exception classes
- Built-in resilience (retry with backoff + jitter, circuit breaker, configurable timeout)
- HTTPS enforced by default, opt-out for local development
- API keys excluded from serialization output (repr, JSON.stringify, etc.)
- Tests mock HTTP responses; no real API calls

## License

See individual SDK directories for license details.
