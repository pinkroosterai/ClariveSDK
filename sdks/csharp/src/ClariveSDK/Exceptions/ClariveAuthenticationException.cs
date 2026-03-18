namespace ClariveSDK.Exceptions;

public class ClariveAuthenticationException : ClariveApiException
{
    public ClariveAuthenticationException(string message)
        : base("UNAUTHORIZED", message, 401) { }
}
