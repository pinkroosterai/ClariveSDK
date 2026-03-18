namespace ClariveSDK;

public class ResilienceOptions
{
    public bool Enabled { get; set; } = true;

    public int MaxRetries { get; set; } = 3;

    public TimeSpan RetryBaseDelay { get; set; } = TimeSpan.FromSeconds(1);

    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
}
