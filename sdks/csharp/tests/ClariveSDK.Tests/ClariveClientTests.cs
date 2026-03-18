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
            new List<Prompt>());

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
}
