using System.Net;

namespace ClariveSDK.Exceptions;

/// <summary>
/// Base exception for errors returned by the Clarive API.
/// Check <see cref="ErrorCode"/> for the machine-readable code and <see cref="HttpStatusCode"/> for the HTTP status.
/// Specific subtypes exist for common error codes:
/// <see cref="ClariveAuthenticationException"/> (401),
/// <see cref="ClariveNotFoundException"/> (404),
/// <see cref="ClariveValidationException"/> (422),
/// <see cref="ClariveRateLimitException"/> (429).
/// </summary>
public class ClariveApiException : HttpRequestException
{
    /// <summary>
    /// The machine-readable error code from the API (e.g. <c>UNAUTHORIZED</c>, <c>NOT_FOUND</c>).
    /// </summary>
    public string ErrorCode { get; }

    /// <summary>
    /// The HTTP status code of the error response.
    /// </summary>
    public new int HttpStatusCode { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="ClariveApiException"/>.
    /// </summary>
    /// <param name="errorCode">The API error code.</param>
    /// <param name="message">The human-readable error message.</param>
    /// <param name="httpStatusCode">The HTTP status code.</param>
    public ClariveApiException(string errorCode, string message, int httpStatusCode)
        : base(message, null, (System.Net.HttpStatusCode)httpStatusCode)
    {
        ErrorCode = errorCode;
        HttpStatusCode = httpStatusCode;
    }

    /// <summary>
    /// Creates the appropriate typed exception for the given API error response.
    /// </summary>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <param name="code">The API error code.</param>
    /// <param name="message">The error message.</param>
    /// <param name="details">Optional per-field validation error details.</param>
    /// <returns>A <see cref="ClariveApiException"/> or a more specific subtype.</returns>
    public static ClariveApiException FromApiError(int statusCode, string code, string message, Dictionary<string, string>? details = null)
    {
        return code switch
        {
            "UNAUTHORIZED" => new ClariveAuthenticationException(message),
            "NOT_FOUND" => new ClariveNotFoundException(message),
            "VALIDATION_ERROR" => new ClariveValidationException(message, details ?? new Dictionary<string, string>()),
            "RATE_LIMITED" => new ClariveRateLimitException(message),
            _ => new ClariveApiException(code, message, statusCode)
        };
    }
}
