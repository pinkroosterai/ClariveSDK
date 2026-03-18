namespace ClariveSDK.Exceptions;

public class ClariveValidationException : ClariveApiException
{
    public IReadOnlyDictionary<string, string> Details { get; }

    public ClariveValidationException(string message, Dictionary<string, string> details)
        : base("VALIDATION_ERROR", message, 422)
    {
        Details = details;
    }
}
