namespace ClariveSDK;

public class ClariveOptions
{
    public string ApiKey { get; set; } = string.Empty;

    public string BaseUrl { get; set; } = "https://app.clarive.com";

    public ResilienceOptions Resilience { get; set; } = new();

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
            throw new ArgumentException("ApiKey is required.", nameof(ApiKey));
    }
}
