namespace ClariveSDK;

public class ClariveOptions
{
    public string ApiKey { get; set; } = string.Empty;

    public string BaseUrl { get; set; } = "https://app.clarive.com";

    public bool AllowInsecureHttp { get; set; }

    public ResilienceOptions Resilience { get; set; } = new();

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
