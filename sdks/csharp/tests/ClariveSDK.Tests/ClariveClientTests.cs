using System.Net;
using System.Text.Json;
using ClariveSDK.Exceptions;
using ClariveSDK.Models;
using ClariveSDK.Tests.Helpers;

namespace ClariveSDK.Tests;

public class ClariveClientTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private const string TestApiKey = "cl_test_key_1234567890abcdef";
    private static readonly Guid TestEntryId = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6");

    private static ClariveOptions CreateOptions() => new()
    {
        ApiKey = TestApiKey,
        BaseUrl = "https://test.clarive.app"
    };

    [Fact]
    public async Task GetEntryAsync_SendsCorrectRequest()
    {
        var expectedEntry = new PromptEntry(
            TestEntryId, "Test Entry", null, 1,
            new List<Prompt>(),
            new List<string> { "test" },
            DateTime.Parse("2026-03-18T10:00:00Z").ToUniversalTime(),
            DateTime.Parse("2026-03-18T10:00:00Z").ToUniversalTime(),
            new List<TabSummary>(),
            0);

        var handler = new MockHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(expectedEntry, JsonOptions),
                    System.Text.Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(handler);
        var client = new ClariveClient(httpClient, CreateOptions());

        var result = await client.GetEntryAsync(TestEntryId);

        Assert.NotNull(handler.LastRequest);
        Assert.Equal(HttpMethod.Get, handler.LastRequest.Method);
        Assert.Equal($"https://test.clarive.app/public/v1/entries/{TestEntryId}",
            handler.LastRequest.RequestUri?.ToString());
        Assert.Equal(TestApiKey,
            handler.LastRequest.Headers.GetValues("X-Api-Key").First());

        Assert.Equal(TestEntryId, result.Id);
        Assert.Equal("Test Entry", result.Title);
    }

    [Fact]
    public async Task GenerateAsync_SendsCorrectRequest()
    {
        var expectedResponse = new GenerateResponse(
            TestEntryId, "Test Entry", 1, "System msg",
            new List<RenderedPrompt> { new("Rendered content", 1) });

        var handler = new MockHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(expectedResponse, JsonOptions),
                    System.Text.Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(handler);
        var client = new ClariveClient(httpClient, CreateOptions());

        var request = new GenerateRequest
        {
            Fields = new Dictionary<string, string> { ["userName"] = "Alice" }
        };

        var result = await client.GenerateAsync(TestEntryId, request);

        Assert.NotNull(handler.LastRequest);
        Assert.Equal(HttpMethod.Post, handler.LastRequest.Method);
        Assert.Equal($"https://test.clarive.app/public/v1/entries/{TestEntryId}/generate",
            handler.LastRequest.RequestUri?.ToString());
        Assert.Equal(TestApiKey,
            handler.LastRequest.Headers.GetValues("X-Api-Key").First());

        Assert.NotNull(handler.LastRequestBody);
        Assert.Contains("\"userName\"", handler.LastRequestBody);
        Assert.Contains("\"Alice\"", handler.LastRequestBody);

        Assert.Equal(TestEntryId, result.Id);
        Assert.Single(result.RenderedPrompts);
        Assert.Equal("Rendered content", result.RenderedPrompts[0].Content);
    }

    [Fact]
    public async Task GetEntryAsync_ForwardsCancellationToken()
    {
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var handler = new MockHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK));

        var httpClient = new HttpClient(handler);
        var client = new ClariveClient(httpClient, CreateOptions());

        await Assert.ThrowsAsync<TaskCanceledException>(
            () => client.GetEntryAsync(TestEntryId, cts.Token));
    }

    [Fact]
    public void Constructor_ThrowsOnMissingApiKey()
    {
        var httpClient = new HttpClient();
        var options = new ClariveOptions { ApiKey = "", BaseUrl = "https://test.clarive.app" };

        Assert.Throws<ArgumentException>(() => new ClariveClient(httpClient, options));
    }

    [Fact]
    public async Task GetEntryAsync_ThrowsOnNonSuccessStatusCode()
    {
        var handler = new MockHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.NotFound));

        var httpClient = new HttpClient(handler);
        var client = new ClariveClient(httpClient, CreateOptions());

        await Assert.ThrowsAsync<ClariveApiException>(
            () => client.GetEntryAsync(TestEntryId));
    }

    [Fact]
    public async Task ListEntriesAsync_ReturnsPaginatedResponse()
    {
        var responseBody = new
        {
            items = new[]
            {
                new
                {
                    id = TestEntryId,
                    title = "Test Entry",
                    version = 1,
                    hasSystemMessage = true,
                    isTemplate = true,
                    isChain = false,
                    promptCount = 1,
                    firstPromptPreview = "Hello {{name}}",
                    tags = new[] { "test" },
                    createdAt = "2026-03-18T10:00:00Z",
                    updatedAt = "2026-03-18T10:00:00Z",
                    tabs = Array.Empty<object>(),
                    tabCount = 0
                }
            },
            totalCount = 1,
            page = 1,
            pageSize = 50
        };

        var handler = new MockHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(responseBody, JsonOptions),
                    System.Text.Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(handler);
        var client = new ClariveClient(httpClient, CreateOptions());

        var result = await client.ListEntriesAsync();

        Assert.NotNull(handler.LastRequest);
        Assert.Equal(HttpMethod.Get, handler.LastRequest.Method);
        Assert.Contains("entries", handler.LastRequest.RequestUri?.ToString() ?? "");

        Assert.Equal(1, result.TotalCount);
        Assert.Equal(1, result.Page);
        Assert.Equal(50, result.PageSize);
        Assert.Single(result.Items);
        Assert.Equal(TestEntryId, result.Items[0].Id);
        Assert.Equal("Test Entry", result.Items[0].Title);
        Assert.True(result.Items[0].HasSystemMessage);
        Assert.True(result.Items[0].IsTemplate);
        Assert.False(result.Items[0].IsChain);
        Assert.Empty(result.Items[0].Tabs);
        Assert.Equal(0, result.Items[0].TabCount);
    }

    [Fact]
    public async Task ListTagsAsync_ReturnsTagList()
    {
        var responseBody = new[]
        {
            new { name = "test", entryCount = 5 },
            new { name = "production", entryCount = 3 }
        };

        var handler = new MockHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(responseBody, JsonOptions),
                    System.Text.Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(handler);
        var client = new ClariveClient(httpClient, CreateOptions());

        var result = await client.ListTagsAsync();

        Assert.NotNull(handler.LastRequest);
        Assert.Equal(HttpMethod.Get, handler.LastRequest.Method);
        Assert.Contains("tags", handler.LastRequest.RequestUri?.ToString() ?? "");

        Assert.Equal(2, result.Count);
        Assert.Equal("test", result[0].Name);
        Assert.Equal(5, result[0].EntryCount);
        Assert.Equal("production", result[1].Name);
        Assert.Equal(3, result[1].EntryCount);
    }

    [Fact]
    public async Task ListTabsAsync_ReturnsTabList()
    {
        var responseBody = new[]
        {
            new { id = Guid.Parse("00000000-0000-0000-0000-000000000010"), name = "Main", isMainTab = true, forkedFromVersion = (int?)null },
            new { id = Guid.Parse("00000000-0000-0000-0000-000000000011"), name = "Formal", isMainTab = false, forkedFromVersion = (int?)3 }
        };

        var handler = new MockHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(responseBody, JsonOptions),
                    System.Text.Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(handler);
        var client = new ClariveClient(httpClient, CreateOptions());

        var result = await client.ListTabsAsync(TestEntryId);

        Assert.NotNull(handler.LastRequest);
        Assert.Equal(HttpMethod.Get, handler.LastRequest.Method);
        Assert.Contains($"entries/{TestEntryId}/tabs", handler.LastRequest.RequestUri?.ToString() ?? "");

        Assert.Equal(2, result.Count);
        Assert.Equal("Main", result[0].Name);
        Assert.True(result[0].IsMainTab);
        Assert.Null(result[0].ForkedFromVersion);
        Assert.Equal("Formal", result[1].Name);
        Assert.False(result[1].IsMainTab);
        Assert.Equal(3, result[1].ForkedFromVersion);
    }

    [Fact]
    public async Task GetTabAsync_SendsCorrectRequest()
    {
        var tabId = Guid.Parse("00000000-0000-0000-0000-000000000010");
        var expectedEntry = new PromptEntry(
            TestEntryId, "Test Entry", null, 1,
            new List<Prompt>(),
            new List<string> { "test" },
            DateTime.Parse("2026-03-18T10:00:00Z").ToUniversalTime(),
            DateTime.Parse("2026-03-18T10:00:00Z").ToUniversalTime(),
            new List<TabSummary>(),
            0);

        var handler = new MockHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(expectedEntry, JsonOptions),
                    System.Text.Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(handler);
        var client = new ClariveClient(httpClient, CreateOptions());

        var result = await client.GetTabAsync(TestEntryId, tabId);

        Assert.NotNull(handler.LastRequest);
        Assert.Equal(HttpMethod.Get, handler.LastRequest.Method);
        Assert.Equal($"https://test.clarive.app/public/v1/entries/{TestEntryId}/tabs/{tabId}",
            handler.LastRequest.RequestUri?.ToString());

        Assert.Equal(TestEntryId, result.Id);
        Assert.Equal("Test Entry", result.Title);
    }

    [Fact]
    public async Task GenerateTabAsync_SendsCorrectRequest()
    {
        var tabId = Guid.Parse("00000000-0000-0000-0000-000000000010");
        var expectedResponse = new GenerateResponse(
            TestEntryId, "Test Entry", 1, null,
            new List<RenderedPrompt> { new("Rendered content", 1) });

        var handler = new MockHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(expectedResponse, JsonOptions),
                    System.Text.Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(handler);
        var client = new ClariveClient(httpClient, CreateOptions());

        var request = new GenerateRequest
        {
            Fields = new Dictionary<string, string> { ["name"] = "Alice" }
        };

        var result = await client.GenerateTabAsync(TestEntryId, tabId, request);

        Assert.NotNull(handler.LastRequest);
        Assert.Equal(HttpMethod.Post, handler.LastRequest.Method);
        Assert.Equal($"https://test.clarive.app/public/v1/entries/{TestEntryId}/tabs/{tabId}/generate",
            handler.LastRequest.RequestUri?.ToString());

        Assert.Equal(TestEntryId, result.Id);
        Assert.Single(result.RenderedPrompts);
        Assert.Equal("Rendered content", result.RenderedPrompts[0].Content);
    }
}
