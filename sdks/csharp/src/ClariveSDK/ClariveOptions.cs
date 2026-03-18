namespace ClariveSDK;

/// <summary>
/// Configuration options for the Clarive SDK.
/// </summary>
public class ClariveOptions
{
    /// <summary>
    /// The API key used to authenticate with the Clarive API.
    /// Keys are prefixed with <c>cl_</c> and created in the Clarive admin UI.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// The base URL of the Clarive instance. Defaults to <c>https://app.clarive.com</c>.
    /// Must be an absolute HTTPS URL unless <see cref="AllowInsecureHttp"/> is set to <c>true</c>.
    /// </summary>
    public string BaseUrl { get; set; } = "https://app.clarive.com";

    /// <summary>
    /// When <c>true</c>, allows HTTP (non-HTTPS) base URLs. Use only for local development.
    /// Defaults to <c>false</c>.
    /// </summary>
    public bool AllowInsecureHttp { get; set; }

    /// <summary>
    /// Resilience options controlling retry, circuit breaker, and timeout behavior.
    /// </summary>
    public ResilienceOptions Resilience { get; set; } = new();

    /// <summary>
    /// Validates that all required options are set and well-formed.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// <see cref="ApiKey"/> is empty, <see cref="BaseUrl"/> is not a valid absolute URL,
    /// or <see cref="BaseUrl"/> uses HTTP without <see cref="AllowInsecureHttp"/> enabled.
    /// </exception>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
            throw new ArgumentException("ApiKey is required.", nameof(ApiKey));

        if (string.IsNullOrWhiteSpace(BaseUrl))
            throw new ArgumentException("BaseUrl is required.", nameof(BaseUrl));

        if (!Uri.TryCreate(BaseUrl, UriKind.Absolute, out var uri))
            throw new ArgumentException("BaseUrl must be a valid absolute URL.", nameof(BaseUrl));

        if (!AllowInsecureHttp && uri.Scheme != Uri.UriSchemeHttps)
            throw new ArgumentException("BaseUrl must use HTTPS. Set AllowInsecureHttp = true for development.", nameof(BaseUrl));
    }
}
