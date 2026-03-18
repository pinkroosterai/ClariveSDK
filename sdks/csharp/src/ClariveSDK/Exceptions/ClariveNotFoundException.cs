namespace ClariveSDK.Exceptions;

/// <summary>
/// Thrown when the API returns <c>404 Not Found</c> — the entry does not exist, is trashed, or has no published version.
/// </summary>
public class ClariveNotFoundException : ClariveApiException
{
    /// <summary>
    /// Initializes a new instance with the given error message.
    /// </summary>
    /// <param name="message">The error message from the API.</param>
    public ClariveNotFoundException(string message)
        : base("NOT_FOUND", message, 404) { }
}
