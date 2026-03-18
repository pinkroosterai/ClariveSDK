namespace ClariveSDK.Exceptions;

/// <summary>
/// Thrown when the API returns <c>429 Too Many Requests</c> — the rate limit has been exceeded.
/// The default limit is 20 requests per minute.
/// </summary>
public class ClariveRateLimitException : ClariveApiException
{
    /// <summary>
    /// Initializes a new instance with the given error message.
    /// </summary>
    /// <param name="message">The error message from the API.</param>
    public ClariveRateLimitException(string message)
        : base("RATE_LIMITED", message, 429) { }
}
