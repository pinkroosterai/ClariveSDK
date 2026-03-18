namespace ClariveSDK;

/// <summary>
/// Options for the built-in resilience pipeline (retry, circuit breaker, timeout).
/// Resilience is enabled by default when using <see cref="ClariveServiceCollectionExtensions.AddClarive(Microsoft.Extensions.DependencyInjection.IServiceCollection, System.Action{ClariveOptions})"/>.
/// Disable it to chain your own Polly policies via the returned <see cref="Microsoft.Extensions.DependencyInjection.IHttpClientBuilder"/>.
/// </summary>
public class ResilienceOptions
{
    /// <summary>
    /// Whether the built-in resilience pipeline is enabled. Defaults to <c>true</c>.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Maximum number of retry attempts for transient failures. Defaults to <c>3</c>.
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Base delay between retries. Actual delay uses exponential backoff with jitter.
    /// Defaults to <c>1 second</c>.
    /// </summary>
    public TimeSpan RetryBaseDelay { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Maximum time to wait for a single request before timing out.
    /// Defaults to <c>30 seconds</c>.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
}
