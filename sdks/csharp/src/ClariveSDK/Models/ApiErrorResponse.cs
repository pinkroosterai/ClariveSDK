namespace ClariveSDK.Models;

internal record ApiErrorResponse(ApiError? Error);

internal record ApiError(
    string Code,
    string Message,
    Dictionary<string, string>? Details);
