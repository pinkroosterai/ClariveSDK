using System.Net;

namespace ClariveSDK.Exceptions;

public class ClariveApiException : HttpRequestException
{
    public string ErrorCode { get; }
    public int HttpStatusCode { get; }

    public ClariveApiException(string errorCode, string message, int httpStatusCode)
        : base(message, null, (System.Net.HttpStatusCode)httpStatusCode)
    {
        ErrorCode = errorCode;
        HttpStatusCode = httpStatusCode;
    }

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
