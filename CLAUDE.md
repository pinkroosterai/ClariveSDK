# ClariveSDK

Monorepo containing official Clarive Public API SDKs in C#, Python, and TypeScript.

## Project Structure

```
sdks/
  csharp/       — .NET 9 class library (ClariveSDK namespace)
  python/       — Python package (clarive-sdk, src layout)
  typescript/   — TypeScript ESM package (clarive-sdk)
```

Each SDK has `src/`, `tests/`, and `example/` directories.

## API Reference

The full Clarive Public API specification lives in `CLARIVE_PUBLIC_API.md` at the repo root. All SDKs wrap these endpoints:

- `GET /public/v1/entries` — list published entries with filtering, search, and pagination
- `GET /public/v1/entries/{entryId}` — retrieve a published prompt entry
- `POST /public/v1/entries/{entryId}/generate` — render a prompt with template variable substitution
- `GET /public/v1/entries/{entryId}/tabs` — list tabs for an entry
- `GET /public/v1/entries/{entryId}/tabs/{tabId}` — retrieve a specific tab
- `POST /public/v1/entries/{entryId}/tabs/{tabId}/generate` — render a tab with template variables
- `GET /public/v1/tags` — list all tags with entry counts

Auth is via `X-Api-Key` header. Responses use camelCase JSON.

## SDK Design Principles

- Each SDK must be idiomatic to its language — do not port patterns across languages
- Expose a single client class that takes an API key and optional base URL
- All HTTP errors must map to typed exceptions/errors with the API error code and message
- Models should match the API response shapes from `CLARIVE_PUBLIC_API.md`
- No external dependencies beyond an HTTP client (httpx for Python, built-in fetch for TS, HttpClient for C#)

## Build & Test

### C# (.NET 9)
```
cd sdks/csharp
dotnet build
dotnet test
dotnet run --project example/ClariveSDK.Example
```

### Python (>=3.10)
```
cd sdks/python
pip install -e ".[dev]"
pytest
python example/main.py
```

### TypeScript (Node >=18)
```
cd sdks/typescript
npm install
npm run build
npm test
npx tsx example/main.ts
```

### All (via Makefile)
```
make build
make test
make clean
```

## Conventions

- C#: follow standard .NET naming (PascalCase types/methods, camelCase locals)
- Python: PEP 8, type hints required, async-first with httpx
- TypeScript: strict mode, ESM, no `any` types
- Tests: unit tests mock HTTP responses; do not call the real API
- Keep SDKs independent — no shared code generation or cross-language tooling

## Clarive Backend

The backend source is at `~/Clarive` (ASP.NET Core). Refer to it when verifying endpoint behavior, but do not modify it from this repo.
