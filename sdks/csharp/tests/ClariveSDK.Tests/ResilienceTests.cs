using Microsoft.Extensions.DependencyInjection;

namespace ClariveSDK.Tests;

public class ResilienceTests
{
    private const string TestApiKey = "cl_test_key_1234567890abcdef";
    private const string TestBaseUrl = "https://test.clarive.app";

    [Fact]
    public void ResilienceOptions_Has_Correct_Defaults()
    {
        var options = new ResilienceOptions();

        Assert.True(options.Enabled);
        Assert.Equal(3, options.MaxRetries);
        Assert.Equal(TimeSpan.FromSeconds(1), options.RetryBaseDelay);
        Assert.Equal(TimeSpan.FromSeconds(30), options.Timeout);
    }

    [Fact]
    public void ClariveOptions_Resilience_Is_Not_Null()
    {
        var options = new ClariveOptions();

        Assert.NotNull(options.Resilience);
        Assert.True(options.Resilience.Enabled);
    }

    [Fact]
    public void AddClarive_With_Resilience_Enabled_Resolves_Client()
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
    public void AddClarive_With_Resilience_Disabled_Resolves_Client()
    {
        var services = new ServiceCollection();

        services.AddClarive(opts =>
        {
            opts.ApiKey = TestApiKey;
            opts.BaseUrl = TestBaseUrl;
            opts.Resilience.Enabled = false;
        });

        var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<ClariveClient>();

        Assert.NotNull(client);
    }

    [Fact]
    public void ResilienceOptions_Custom_Values()
    {
        var options = new ResilienceOptions
        {
            Enabled = true,
            MaxRetries = 5,
            RetryBaseDelay = TimeSpan.FromMilliseconds(500)
        };

        Assert.Equal(5, options.MaxRetries);
        Assert.Equal(TimeSpan.FromMilliseconds(500), options.RetryBaseDelay);
    }
}
