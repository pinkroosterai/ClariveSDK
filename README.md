# ClariveSDK

Official SDKs for the [Clarive](https://github.com/pinkroosterai/Clarive) Public API. Three languages, four endpoints: list and search entries, fetch a single entry, render it with your variables, and browse tags. Each SDK handles typed errors, retries, and circuit breaking out of the box.

| Language | Package | Install | Runtime |
|----------|---------|---------|---------|
| [C#](sdks/csharp/) | [`ClariveSDK`](https://www.nuget.org/packages/ClariveSDK) | `dotnet add package ClariveSDK` | .NET 9+ |
| [Python](sdks/python/) | [`clarive-sdk`](https://pypi.org/project/clarive-sdk/) | `pip install clarive-sdk` | Python 3.10+ |
| [TypeScript](sdks/typescript/) | [`clarive-sdk`](https://www.npmjs.com/package/clarive-sdk) | `npm install clarive-sdk` | Node 18+ |

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

## API

Four endpoints, all behind an `X-Api-Key` header. Full spec in [CLARIVE_PUBLIC_API.md](CLARIVE_PUBLIC_API.md).

| Method | Endpoint | What it does |
|--------|----------|-------------|
| `GET` | `/public/v1/entries` | List published entries with filtering, search, and pagination |
| `GET` | `/public/v1/entries/{entryId}` | Retrieve a published prompt entry |
| `POST` | `/public/v1/entries/{entryId}/generate` | Render it with template variables |
| `GET` | `/public/v1/tags` | List all tags with entry counts |

## What the SDKs give you

Every SDK is written to feel native in its language. They share principles, not code.

- A single client class. Pass your API key, get back typed responses.
- Errors you can catch by name: `ClariveNotFoundError`, `ClariveValidationError`, `ClariveRateLimitError`. No parsing status codes.
- Retry with exponential backoff and jitter, a circuit breaker that stops hammering failing services, and configurable timeouts. All on by default, all easy to turn off.
- HTTPS enforced unless you explicitly opt out for local dev.
- API keys stay out of logs. Python's `repr()`, TypeScript's `JSON.stringify()`, and C#'s serialization all omit credentials.

## Build and test

```bash
make build    # All three SDKs
make test     # All test suites (144 tests across C#, Python, TypeScript)
make clean    # Remove build artifacts
```

Or pick one:

```bash
make test-csharp
make test-python
make test-typescript
```

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md).

## License

[MIT](LICENSE)
