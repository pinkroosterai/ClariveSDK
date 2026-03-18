using ClariveSDK;
using ClariveSDK.Models;

// Configure the SDK with your API key and Clarive instance URL
var options = new ClariveOptions
{
    ApiKey = "cl_your_api_key_here",
    BaseUrl = "https://demo.clarive.app"
};

// Create an HttpClient and ClariveClient
// (For ASP.NET Core apps, use AddClarive() DI integration instead)
using var httpClient = new HttpClient();
var client = new ClariveClient(httpClient, options);

// List published entries with filtering and pagination
Console.WriteLine("=== List Entries ===");
var list = await client.ListEntriesAsync(new ListEntriesOptions
{
    PageSize = 5,
    SortBy = "recent"
});
Console.WriteLine($"Total entries: {list.TotalCount} (showing page {list.Page}, {list.Items.Count} items)");

foreach (var summary in list.Items)
{
    Console.WriteLine($"  {summary.Title} (v{summary.Version}) — {summary.PromptCount} prompt(s), tags: [{string.Join(", ", summary.Tags)}]");
}

// List all tags
Console.WriteLine("\n=== List Tags ===");
var tags = await client.ListTagsAsync();
foreach (var tag in tags)
    Console.WriteLine($"  {tag.Name}: {tag.EntryCount} entries");

// Get a single entry by ID (use the first from the list, or a known ID)
var entryId = list.Items.Count > 0
    ? list.Items[0].Id
    : Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6");

Console.WriteLine("\n=== Get Entry ===");
var entry = await client.GetEntryAsync(entryId);
Console.WriteLine($"Title: {entry.Title}");
Console.WriteLine($"Version: {entry.Version}");
Console.WriteLine($"System Message: {entry.SystemMessage ?? "(none)"}");
Console.WriteLine($"Tags: [{string.Join(", ", entry.Tags)}]");
Console.WriteLine($"Updated: {entry.UpdatedAt:yyyy-MM-dd HH:mm}");
Console.WriteLine($"Published: {entry.PublishedAt?.ToString("yyyy-MM-dd HH:mm") ?? "(not published)"}");
Console.WriteLine($"Prompts: {entry.Prompts.Count}");

foreach (var prompt in entry.Prompts)
{
    Console.WriteLine($"\n  Prompt #{prompt.Order}: {(prompt.IsTemplate ? "[template]" : "[static]")}");
    Console.WriteLine($"  Content: {prompt.Content[..Math.Min(80, prompt.Content.Length)]}...");

    if (prompt.TemplateFields is not null)
    {
        foreach (var field in prompt.TemplateFields)
            Console.WriteLine($"    Field: {field.Name} ({field.Type})");
    }
}

// Generate rendered prompts with template variable values
Console.WriteLine("\n=== Generate ===");
var request = new GenerateRequest
{
    Fields = new Dictionary<string, string>
    {
        ["companyName"] = "Acme Corp",
        ["customerMessage"] = "I need help with my order #12345"
    }
};

var result = await client.GenerateAsync(entryId, request);
Console.WriteLine($"Title: {result.Title}");
Console.WriteLine($"System Message: {result.SystemMessage ?? "(none)"}");

foreach (var rendered in result.RenderedPrompts)
{
    Console.WriteLine($"\n  Rendered Prompt #{rendered.Order}:");
    Console.WriteLine($"  {rendered.Content}");
}
