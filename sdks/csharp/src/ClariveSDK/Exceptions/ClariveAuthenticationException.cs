namespace ClariveSDK.Exceptions;

/// <summary>
/// Thrown when the API returns <c>401 Unauthorized</c> — the API key is invalid or missing.
/// </summary>
public class ClariveAuthenticationException : ClariveApiException
{
    /// <summary>
    /// Initializes a new instance with the given error message.
    /// </summary>
    /// <param name="message">The error message from the API.</param>
    public ClariveAuthenticationException(string message)
        : base("UNAUTHORIZED", message, 401) { }
}
