using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ClariveSDK;

public static class ClariveServiceCollectionExtensions
{
    public static IHttpClientBuilder AddClarive(
        this IServiceCollection services, Action<ClariveOptions> configure)
    {
        services.Configure(configure);
        return AddClariveCore(services);
    }

    public static IHttpClientBuilder AddClarive(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ClariveOptions>(configuration);
        return AddClariveCore(services);
    }

    private static IHttpClientBuilder AddClariveCore(IServiceCollection services)
    {
        services.AddTransient<ApiKeyDelegatingHandler>();

        return services
            .AddHttpClient<ClariveClient>((sp, client) =>
            {
                var options = sp.GetRequiredService<IOptions<ClariveOptions>>().Value;
                options.Validate();
                var baseUrl = options.BaseUrl.TrimEnd('/');
                client.BaseAddress = new Uri($"{baseUrl}/public/v1/");
            })
            .AddHttpMessageHandler<ApiKeyDelegatingHandler>();
    }
}
