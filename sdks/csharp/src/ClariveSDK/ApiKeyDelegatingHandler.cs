using Microsoft.Extensions.Options;

namespace ClariveSDK;

/// <summary>
/// HTTP message handler that injects the <c>X-Api-Key</c> header into outgoing requests.
/// Used automatically when the SDK is registered via <see cref="ClariveServiceCollectionExtensions.AddClarive(Microsoft.Extensions.DependencyInjection.IServiceCollection, System.Action{ClariveOptions})"/>.
/// </summary>
public class ApiKeyDelegatingHandler : DelegatingHandler
{
    private readonly IOptions<ClariveOptions> _options;

    /// <summary>
    /// Initializes a new instance with the given options.
    /// </summary>
    /// <param name="options">The SDK configuration containing the API key.</param>
    public ApiKeyDelegatingHandler(IOptions<ClariveOptions> options)
    {
        _options = options;
    }

    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!request.Headers.Contains("X-Api-Key"))
        {
            request.Headers.Add("X-Api-Key", _options.Value.ApiKey);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
