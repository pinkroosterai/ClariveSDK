using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Polly;

namespace ClariveSDK;

public static class ClariveServiceCollectionExtensions
{
    public static IHttpClientBuilder AddClarive(
        this IServiceCollection services, Action<ClariveOptions> configure)
    {
        services.Configure(configure);
        return AddClariveCore(services, configure);
    }

    public static IHttpClientBuilder AddClarive(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ClariveOptions>(configuration);

        var options = new ClariveOptions();
        configuration.Bind(options);

        return AddClariveCore(services, _ => { }, options);
    }

    private static IHttpClientBuilder AddClariveCore(
        IServiceCollection services,
        Action<ClariveOptions> configure,
        ClariveOptions? prebuiltOptions = null)
    {
        services.AddTransient<ApiKeyDelegatingHandler>();

        var builder = services
            .AddHttpClient<ClariveClient>((sp, client) =>
            {
                var options = sp.GetRequiredService<IOptions<ClariveOptions>>().Value;
                options.Validate();
                var baseUrl = options.BaseUrl.TrimEnd('/');
                client.BaseAddress = new Uri($"{baseUrl}/public/v1/");
            })
            .AddHttpMessageHandler<ApiKeyDelegatingHandler>();

        var resilienceOptions = prebuiltOptions?.Resilience ?? GetResilienceOptions(configure);

        if (resilienceOptions.Enabled)
        {
            builder.AddResilienceHandler("clarive-resilience", (pipelineBuilder, context) =>
            {
                var opts = context.ServiceProvider
                    .GetRequiredService<IOptions<ClariveOptions>>().Value.Resilience;

                pipelineBuilder.AddRetry(new HttpRetryStrategyOptions
                {
                    MaxRetryAttempts = opts.MaxRetries,
                    Delay = opts.RetryBaseDelay,
                    BackoffType = DelayBackoffType.Exponential,
                    UseJitter = true
                });

                pipelineBuilder.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions());

                pipelineBuilder.AddTimeout(TimeSpan.FromSeconds(30));
            });
        }

        return builder;
    }

    private static ResilienceOptions GetResilienceOptions(Action<ClariveOptions> configure)
    {
        var options = new ClariveOptions();
        configure(options);
        return options.Resilience;
    }
}
