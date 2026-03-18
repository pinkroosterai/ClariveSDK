namespace ClariveSDK.Exceptions;

/// <summary>
/// Thrown when the API returns <c>422 Unprocessable Entity</c> — one or more template field values are invalid.
/// Check <see cref="Details"/> for per-field error messages.
/// </summary>
public class ClariveValidationException : ClariveApiException
{
    /// <summary>
    /// Per-field validation errors. Keys are field names, values are error messages
    /// (e.g. <c>"Field 'age' must be a valid integer."</c>).
    /// </summary>
    public IReadOnlyDictionary<string, string> Details { get; }

    /// <summary>
    /// Initializes a new instance with the given error message and field-level details.
    /// </summary>
    /// <param name="message">The error message from the API.</param>
    /// <param name="details">Per-field validation errors.</param>
    public ClariveValidationException(string message, Dictionary<string, string> details)
        : base("VALIDATION_ERROR", message, 422)
    {
        Details = details;
    }
}
