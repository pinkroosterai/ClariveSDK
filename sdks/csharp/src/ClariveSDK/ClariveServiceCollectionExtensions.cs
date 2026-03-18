using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Polly;

namespace ClariveSDK;

/// <summary>
/// Extension methods for registering the Clarive SDK with the dependency injection container.
/// </summary>
public static class ClariveServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="ClariveClient"/> and <see cref="IClariveClient"/> with the DI container.
    /// Configures the underlying <see cref="HttpClient"/>, API key handler, and optional resilience pipeline.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">A delegate to configure <see cref="ClariveOptions"/>.</param>
    /// <returns>
    /// An <see cref="IHttpClientBuilder"/> that can be used to chain additional configuration
    /// such as custom delegating handlers or Polly policies.
    /// </returns>
    /// <example>
    /// <code>
    /// services.AddClarive(opts =>
    /// {
    ///     opts.ApiKey = "cl_your_key";
    ///     opts.BaseUrl = "https://demo.clarive.app";
    /// });
    /// </code>
    /// </example>
    public static IHttpClientBuilder AddClarive(
        this IServiceCollection services, Action<ClariveOptions> configure)
    {
        services.Configure(configure);
        return AddClariveCore(services, configure);
    }

    /// <summary>
    /// Registers <see cref="ClariveClient"/> and <see cref="IClariveClient"/> with the DI container,
    /// binding options from an <see cref="IConfiguration"/> section (e.g. <c>"Clarive"</c> in appsettings.json).
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration section to bind <see cref="ClariveOptions"/> from.</param>
    /// <returns>
    /// An <see cref="IHttpClientBuilder"/> that can be used to chain additional configuration.
    /// </returns>
    /// <example>
    /// <code>
    /// services.AddClarive(builder.Configuration.GetSection("Clarive"));
    /// </code>
    /// </example>
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
        services.AddTransient<IClariveClient>(sp => sp.GetRequiredService<ClariveClient>());

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

                pipelineBuilder.AddTimeout(opts.Timeout);
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
