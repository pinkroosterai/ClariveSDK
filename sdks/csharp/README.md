# ClariveSDK for .NET

[![NuGet](https://img.shields.io/nuget/v/ClariveSDK)](https://www.nuget.org/packages/ClariveSDK)

The official C# SDK for the [Clarive](https://clarive.com) Public API. Retrieve prompt entries, render templates with variable substitution, and integrate it all into your .NET apps with a single `AddClarive()` call.

Targets **.NET 9+**. No dependencies beyond `Microsoft.Extensions.Http` and `Microsoft.Extensions.Http.Resilience`.

## Install

```
dotnet add package ClariveSDK
```

## Quick start

```csharp
using ClariveSDK;
using ClariveSDK.Models;

var options = new ClariveOptions
{
    ApiKey = "cl_your_api_key",
    BaseUrl = "https://app.clarive.com"
};

using var httpClient = new HttpClient();
var client = new ClariveClient(httpClient, options);

// Fetch a published prompt entry
var entry = await client.GetEntryAsync(entryId);

// Render it with template variables
var response = await client.GenerateAsync(entryId, new GenerateRequest
{
    Fields = new Dictionary<string, string>
    {
        ["companyName"] = "Acme Corp",
        ["customerMessage"] = "I need help with my order"
    }
});

foreach (var prompt in response.RenderedPrompts)
    Console.WriteLine(prompt.Content);
```

## Dependency injection

For ASP.NET Core or any host that uses `Microsoft.Extensions.DependencyInjection`, register the SDK in one line:

```csharp
builder.Services.AddClarive(opts =>
{
    opts.ApiKey = builder.Configuration["Clarive:ApiKey"]!;
    opts.BaseUrl = "https://app.clarive.com";
});
```

Or bind directly from configuration:

```csharp
builder.Services.AddClarive(builder.Configuration.GetSection("Clarive"));
```

```json
{
  "Clarive": {
    "ApiKey": "cl_your_api_key",
    "BaseUrl": "https://app.clarive.com"
  }
}
```

Then inject `IClariveClient` wherever you need it:

```csharp
public class PromptService(IClariveClient clarive)
{
    public async Task<string> RenderAsync(Guid entryId, Dictionary<string, string> fields)
    {
        var result = await clarive.GenerateAsync(entryId, new GenerateRequest { Fields = fields });
        return result.RenderedPrompts.First().Content;
    }
}
```

The DI registration uses `IHttpClientFactory` under the hood, so you get proper `HttpClient` lifecycle management for free.

## Resilience

Built-in retry, circuit breaker, and timeout are enabled by default when using `AddClarive()`. The defaults:

| Setting | Default |
|---------|---------|
| Max retries | 3 |
| Retry base delay | 1 second (exponential backoff + jitter) |
| Timeout | 30 seconds |
| Circuit breaker | Standard HTTP defaults |

Override them:

```csharp
builder.Services.AddClarive(opts =>
{
    opts.ApiKey = "cl_your_api_key";
    opts.Resilience.MaxRetries = 5;
    opts.Resilience.Timeout = TimeSpan.FromSeconds(60);
});
```

Or disable the built-in pipeline entirely and bring your own Polly policies:

```csharp
builder.Services.AddClarive(opts =>
{
    opts.ApiKey = "cl_your_api_key";
    opts.Resilience.Enabled = false;
});
```

## Error handling

API errors throw typed exceptions, all extending `ClariveApiException`:

| Exception | HTTP status | When |
|-----------|-------------|------|
| `ClariveAuthenticationException` | 401 | Invalid or missing API key |
| `ClariveNotFoundException` | 404 | Entry doesn't exist or is trashed |
| `ClariveValidationException` | 422 | Bad template field values |
| `ClariveRateLimitException` | 429 | Too many requests (limit: 600/min) |

`ClariveValidationException` carries a `Details` dictionary mapping field names to error messages, so you can tell the user exactly what went wrong.

```csharp
try
{
    var result = await client.GenerateAsync(entryId, request);
}
catch (ClariveValidationException ex)
{
    foreach (var (field, error) in ex.Details)
        Console.WriteLine($"{field}: {error}");
}
catch (ClariveRateLimitException)
{
    // Back off and retry
}
catch (ClariveApiException ex)
{
    Console.WriteLine($"API error {ex.ErrorCode}: {ex.Message}");
}
```

## API reference

### `IClariveClient`

| Method | Returns | Description |
|--------|---------|-------------|
| `GetEntryAsync(Guid entryId, CancellationToken)` | `PromptEntry` | Fetches the published version of a prompt entry |
| `GenerateAsync(Guid entryId, GenerateRequest, CancellationToken)` | `GenerateResponse` | Renders prompts with template variable substitution |
| `ListEntriesAsync(ListEntriesOptions?, CancellationToken)` | `PaginatedResponse<EntrySummary>` | Lists published entries with filtering, search, and pagination |
| `ListTagsAsync(CancellationToken)` | `IReadOnlyList<TagInfo>` | Lists all tags with entry counts |
| `ListTabsAsync(Guid entryId, CancellationToken)` | `IReadOnlyList<TabSummary>` | Lists tabs for an entry |
| `GetTabAsync(Guid entryId, Guid tabId, CancellationToken)` | `PromptEntry` | Retrieves a specific tab |
| `GenerateTabAsync(Guid entryId, Guid tabId, GenerateRequest, CancellationToken)` | `GenerateResponse` | Renders a tab with template variable substitution |

### Models

**`PromptEntry`** — a published prompt entry containing one or more prompts.

- `Id` (Guid), `Title` (string), `Version` (int), `SystemMessage` (string?), `Prompts` (list of `Prompt`), `Tags` (list of string), `UpdatedAt` (DateTime), `PublishedAt` (DateTime?), `Tabs` (list of `TabSummary`), `TabCount` (int)

**`Prompt`** — a single prompt within an entry.

- `Content` (string), `Order` (int), `IsTemplate` (bool), `TemplateFields` (list of `TemplateField`?)

**`TemplateField`** — defines a `{{variable}}` placeholder in a template prompt.

- `Id` (Guid), `PromptId` (Guid), `Name`, `Type` (string/int/float/enum), `EnumValues`, `DefaultValue`, `Min`, `Max`

**`GenerateRequest`** — the request body for `GenerateAsync`.

- `Fields` (Dictionary<string, string>?) — keys are field names, values are the substitutions

**`GenerateResponse`** — rendered output from `GenerateAsync`.

- `Id`, `Title`, `Version`, `SystemMessage`, `RenderedPrompts` (list of `RenderedPrompt`)

**`RenderedPrompt`** — a prompt with all template variables replaced.

- `Content` (string), `Order` (int)

**`EntrySummary`** — compact entry representation from `ListEntriesAsync`.

- `Id`, `Title`, `Version`, `HasSystemMessage`, `IsTemplate`, `IsChain`, `PromptCount`, `FirstPromptPreview`, `Tags`, `CreatedAt`, `UpdatedAt`, `Tabs` (list of `TabSummary`), `TabCount` (int)

**`TabSummary`** — summary of a tab on a prompt entry.

- `Id` (Guid), `Name` (string), `IsMainTab` (bool), `ForkedFromVersion` (int?)

**`PaginatedResponse<T>`** — pagination wrapper.

- `Items` (list of T), `TotalCount` (int), `Page` (int), `PageSize` (int)

**`TagInfo`** — a tag with its entry count.

- `Name` (string), `EntryCount` (int)

**`ListEntriesOptions`** — query parameters for `ListEntriesAsync`.

- `FolderId`, `Tags`, `TagMode`, `Page`, `PageSize`, `Search`, `SortBy` (all optional)

## Configuration

`ClariveOptions` controls SDK behavior:

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `ApiKey` | string | `""` | Your API key (required, starts with `cl_`) |
| `BaseUrl` | string | `https://app.clarive.com` | Clarive instance URL |
| `AllowInsecureHttp` | bool | `false` | Permit HTTP URLs (local dev only) |
| `Resilience` | `ResilienceOptions` | Enabled | Retry, circuit breaker, and timeout settings |

HTTPS is enforced by default. If you're developing against a local instance on `http://localhost`, set `AllowInsecureHttp = true`.

## Build and test

```bash
cd sdks/csharp
dotnet build
dotnet test
dotnet run --project example/ClariveSDK.Example
```

## License

See the [repository root](../../) for license information.
