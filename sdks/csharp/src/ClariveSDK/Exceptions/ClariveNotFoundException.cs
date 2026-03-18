namespace ClariveSDK.Exceptions;

public class ClariveNotFoundException : ClariveApiException
{
    public ClariveNotFoundException(string message)
        : base("NOT_FOUND", message, 404) { }
}
