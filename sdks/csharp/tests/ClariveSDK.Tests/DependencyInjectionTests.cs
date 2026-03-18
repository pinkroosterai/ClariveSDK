using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ClariveSDK.Tests.Helpers;

namespace ClariveSDK.Tests;

public class DependencyInjectionTests
{
    private const string TestApiKey = "cl_test_key_1234567890abcdef";
    private const string TestBaseUrl = "https://test.clarive.app";

    [Fact]
    public void AddClarive_Registers_ClariveClient()
    {
        var services = new ServiceCollection();

        services.AddClarive(opts =>
        {
            opts.ApiKey = TestApiKey;
            opts.BaseUrl = TestBaseUrl;
        });

        var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<ClariveClient>();

        Assert.NotNull(client);
    }

    [Fact]
    public void AddClarive_Configures_Options()
    {
        var services = new ServiceCollection();

        services.AddClarive(opts =>
        {
            opts.ApiKey = TestApiKey;
            opts.BaseUrl = TestBaseUrl;
        });

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<ClariveOptions>>();

        Assert.Equal(TestApiKey, options.Value.ApiKey);
        Assert.Equal(TestBaseUrl, options.Value.BaseUrl);
    }

    [Fact]
    public void AddClarive_Returns_IHttpClientBuilder()
    {
        var services = new ServiceCollection();

        var builder = services.AddClarive(opts =>
        {
            opts.ApiKey = TestApiKey;
            opts.BaseUrl = TestBaseUrl;
        });

        Assert.NotNull(builder);
    }

    [Fact]
    public async Task ApiKeyDelegatingHandler_Adds_Header()
    {
        var options = Options.Create(new ClariveOptions
        {
            ApiKey = TestApiKey,
            BaseUrl = TestBaseUrl
        });

        HttpRequestMessage? capturedRequest = null;
        var innerHandler = new MockHttpMessageHandler(req =>
        {
            capturedRequest = req;
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK);
        });

        var handler = new ApiKeyDelegatingHandler(options)
        {
            InnerHandler = innerHandler
        };

        using var httpClient = new HttpClient(handler);
        await httpClient.GetAsync("https://test.clarive.app/test");

        Assert.NotNull(capturedRequest);
        Assert.True(capturedRequest.Headers.Contains("X-Api-Key"));
        Assert.Equal(TestApiKey, capturedRequest.Headers.GetValues("X-Api-Key").First());
    }

    [Fact]
    public async Task ApiKeyDelegatingHandler_Does_Not_Override_Existing_Header()
    {
        var options = Options.Create(new ClariveOptions
        {
            ApiKey = TestApiKey,
            BaseUrl = TestBaseUrl
        });

        HttpRequestMessage? capturedRequest = null;
        var innerHandler = new MockHttpMessageHandler(req =>
        {
            capturedRequest = req;
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK);
        });

        var handler = new ApiKeyDelegatingHandler(options)
        {
            InnerHandler = innerHandler
        };

        using var httpClient = new HttpClient(handler);
        var request = new HttpRequestMessage(HttpMethod.Get, "https://test.clarive.app/test");
        request.Headers.Add("X-Api-Key", "cl_existing_key");
        await httpClient.SendAsync(request);

        Assert.NotNull(capturedRequest);
        Assert.Equal("cl_existing_key", capturedRequest.Headers.GetValues("X-Api-Key").First());
    }
}
