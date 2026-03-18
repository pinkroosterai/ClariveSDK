---
name: sync-api
description: >-
  Detect changes in the Clarive backend Public API and update the SDKs to match.
  Reads the backend source at ~/Clarive, compares with the current SDK models,
  errors, and API spec, then applies changes across C#, Python, and TypeScript.
  Use when the user says "sync the API", "update SDKs from backend", "API changed",
  "check for API drift", or "sync-api".
argument-hint: "[--dry-run] [csharp|python|typescript...]"
---

# Sync SDKs with Clarive Public API

Read the Clarive backend source to detect Public API changes, then update the SDK
models, error handling, and API spec document to match.

## Arguments

Parse `$ARGUMENTS`:
- **`--dry-run`**: Report what changed without modifying any files. Remove flag before parsing rest.
- **SDK filter**: `csharp`, `python`, `typescript` (space-separated). If omitted, sync all three.

## Backend Source Files

The Clarive Public API contract is defined by these files in `~/Clarive/src/backend/Clarive.Api/`:

| What | File |
|------|------|
| Endpoints (routes, handlers) | `Endpoints/PublicApiEndpoints.cs` |
| Response: PromptEntry | `Models/Responses/PublicPromptEntry.cs` |
| Response: GenerateResponse | `Models/Responses/PublicGenerateResponse.cs` |
| Response: Error | `Models/Responses/ErrorResponse.cs` |
| Request: Generate | `Models/Requests/PublicGenerateRequest.cs` |
| Entity: TemplateField | `Models/Entities/TemplateField.cs` |
| Enum: TemplateFieldType | `Models/Enums/TemplateFieldType.cs` |
| Error codes | `Helpers/DomainErrors.cs` |
| Field validation | `Helpers/TemplateFieldValidator.cs` |
| Template rendering | `Services/TemplateParser.cs` |
| Auth handler | `Auth/ApiKeyAuthHandler.cs` |
| Rate limiting | `Program.cs` (search for `AddPolicy("auth"`) |

## Step 1: Read the Backend API Contract

Read ALL the backend files listed above. From each, extract:

### From PublicApiEndpoints.cs:
- Route paths and HTTP methods
- Which response types are returned
- Which error codes are used inline (e.g. `ctx.ErrorResult(401, "UNAUTHORIZED", ...)`)

### From PublicPromptEntry.cs + PublicGenerateResponse.cs:
- Every property name, type, and nullability
- Nested record types (PublicPrompt, RenderedPrompt)
- Note: the API serializes with camelCase (ASP.NET default)

### From TemplateField.cs + TemplateFieldType.cs:
- All properties on TemplateField (Id, PromptId, Name, Type, EnumValues, DefaultValue, Min, Max)
- All enum values in TemplateFieldType (String, Int, Float, Enum)

### From PublicGenerateRequest.cs:
- Request body shape (Fields dictionary)

### From DomainErrors.cs:
- Every error code that the public API can return
- Focus on codes used in `PublicApiEndpoints.cs` and `EntryService.GetPublishedEntryAsync`
- Currently: UNAUTHORIZED, ENTRY_NOT_FOUND, NO_PUBLISHED_VERSION, VALIDATION_ERROR, RATE_LIMITED

### From TemplateFieldValidator.cs:
- Validation rules (required fields, max length, type-specific validation)
- The max field value length constant

### From Program.cs (rate limiting):
- Permit limit, window duration, partition key

## Step 2: Read the Current SDK State

For each SDK being synced, read the corresponding files:

### C# SDK (`sdks/csharp/src/ClariveSDK/`):
- `Models/PromptEntry.cs`, `Models/Prompt.cs`, `Models/TemplateField.cs`
- `Models/GenerateRequest.cs`, `Models/GenerateResponse.cs`, `Models/RenderedPrompt.cs`
- `Exceptions/ClariveApiException.cs` (the `FromApiError` switch statement)

### Python SDK (`sdks/python/src/clarive/`):
- `models.py` — all dataclass definitions and `from_dict()` methods
- `exceptions.py` — the `from_response()` match statement

### TypeScript SDK (`sdks/typescript/src/`):
- `models.ts` — all interface definitions
- `errors.ts` — the `fromResponse()` switch statement

Also read:
- `CLARIVE_PUBLIC_API.md` — the API spec document at the repo root

## Step 3: Detect Drift

Compare backend vs SDK state. Check for drift in these categories:

### 3a: Model Drift
For each response/request type, compare:
- Missing properties (backend added a new field, SDK doesn't have it)
- Removed properties (backend removed a field, SDK still has it)
- Type changes (e.g. `string` → `int`, nullable → required)
- New nested types (backend added a new record type)

Map property names between languages:
- Backend (C# PascalCase) → SDK C# (same), Python (snake_case), TypeScript (camelCase)
- Example: `SystemMessage` → `SystemMessage` / `system_message` / `systemMessage`

### 3b: Error Code Drift
Compare the set of error codes in the backend's `DomainErrors.cs` and inline usage
with the SDK error factories:
- New error codes the SDKs don't handle
- Removed error codes the SDKs still reference
- Changed HTTP status codes for existing error codes

### 3c: Endpoint Drift
- New endpoints added to `PublicApiEndpoints.cs`
- Changed route patterns
- Changed HTTP methods

### 3d: Validation Rule Drift
- Changed max field length
- New validation rules
- Changed type validation behavior

### 3e: Rate Limit Drift
- Changed permit limit or window

## Step 4: Report Changes

Present a clear summary of all detected drift:

```
## API Sync Report

### Model Changes
| Change | Backend | C# SDK | Python SDK | TypeScript SDK |
|--------|---------|--------|------------|----------------|
| Added field `Foo` on `PromptEntry` | ✓ | Missing | Missing | Missing |
| ...

### Error Code Changes
| Code | Backend Status | SDK Status |
|------|----------------|------------|
| NEW_CODE | 404 (new) | Not mapped |
| ...

### Endpoint Changes
- (none, or list)

### Validation Changes
- (none, or list)

### No Changes Detected
- (list categories with no drift)
```

**If `--dry-run`**: Stop here. Report what would change without modifying anything.

## Step 5: Apply Changes

For each detected drift, apply the fix to all selected SDKs:

### Adding a new model property:
1. **C#**: Add the property to the record definition. Follow existing naming (PascalCase).
   Update xmldoc if present.
2. **Python**: Add the field to the dataclass. Add the `from_dict()` mapping
   (camelCase key → snake_case field). Maintain `frozen=True, slots=True`.
3. **TypeScript**: Add the property to the interface. Use camelCase.

### Adding a new error code:
1. **C#**: Add a case to the `FromApiError` switch in `ClariveApiException.cs`.
   If it maps to an existing exception type (e.g. another 404 variant), add it to
   that case's `or` pattern. If it needs a new exception class, create one.
2. **Python**: Add a case to the `from_response()` match in `exceptions.py`.
   Use `|` for multiple codes mapping to the same exception.
3. **TypeScript**: Add a case to `fromResponse()` switch in `errors.ts`.
   Use fallthrough for multiple codes mapping to the same class.

### Adding a new endpoint:
1. Add a new method to the client class in each SDK.
2. Add corresponding request/response models if they don't exist.
3. Add the endpoint to CLARIVE_PUBLIC_API.md.

### Updating CLARIVE_PUBLIC_API.md:
For any change detected, update the API spec document to match the current
backend state. This is the source-of-truth document for SDK consumers.

## Step 6: Update Tests

For each SDK that was modified:

1. **Check if existing test fixtures need updating** (e.g. a new required field
   was added to a model — test JSON fixtures need the field too).
2. **Add tests for new functionality** (new endpoint → new client test,
   new error code → new error mapping test).
3. **Run the test suite** to verify everything passes:
   - C#: `cd sdks/csharp && dotnet test`
   - Python: `cd sdks/python && uv run pytest`
   - TypeScript: `cd sdks/typescript && npx vitest run`

If tests fail, fix the issue before proceeding.

## Step 7: Lint and Type Check

Run language-specific checks:
- Python: `uv run ruff check src/ tests/ && uv run mypy --strict src/`
- TypeScript: `npx biome check src/ tests/ && npx tsc --noEmit`
- C#: `dotnet build` (warnings are errors)

Fix any issues found.

## Step 8: Summary

```
## Sync Complete

### Changes Applied
- {description of each change}

### Files Modified
| SDK | Files |
|-----|-------|
| C# | {list} |
| Python | {list} |
| TypeScript | {list} |
| API Spec | CLARIVE_PUBLIC_API.md |

### Tests
- C#: {N} passed
- Python: {N} passed
- TypeScript: {N} passed

### Next Steps
- Review the changes: `git diff`
- Commit: `git add -A && git commit -m "chore: sync SDKs with backend API changes"`
- Release: `/release {version}`
```

Do NOT commit automatically. Let the user review the diff first.

## Constraints

- NEVER modify files in `~/Clarive/` — the backend is read-only from this repo's perspective
- NEVER guess at backend changes — always read the actual source files
- NEVER remove SDK features that the backend still supports
- If the backend removed something the SDKs depend on, warn loudly but don't delete
- Keep all three SDKs in sync — if you add a field to one, add it to all
- Maintain language idioms: PascalCase for C#, snake_case for Python, camelCase for TypeScript
- Update CLARIVE_PUBLIC_API.md for every contract change — it's the consumer-facing spec
- Run tests after every change — don't accumulate untested modifications
