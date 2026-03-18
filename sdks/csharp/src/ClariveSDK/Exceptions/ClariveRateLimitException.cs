namespace ClariveSDK.Exceptions;

public class ClariveRateLimitException : ClariveApiException
{
    public ClariveRateLimitException(string message)
        : base("RATE_LIMITED", message, 429) { }
}
