using Microsoft.Extensions.Options;

namespace ClariveSDK;

public class ApiKeyDelegatingHandler : DelegatingHandler
{
    private readonly IOptions<ClariveOptions> _options;

    public ApiKeyDelegatingHandler(IOptions<ClariveOptions> options)
    {
        _options = options;
    }

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
