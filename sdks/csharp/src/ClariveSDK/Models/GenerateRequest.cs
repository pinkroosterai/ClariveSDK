namespace ClariveSDK.Models;

/// <summary>
/// Request body for <see cref="IClariveClient.GenerateAsync"/>.
/// </summary>
public class GenerateRequest
{
    /// <summary>
    /// Template variable values to substitute into the prompts.
    /// Keys are field names (matching <c>{{name}}</c> placeholders), values are the substitution strings.
    /// All values are strings; numeric and enum fields are validated server-side.
    /// </summary>
    public Dictionary<string, string>? Fields { get; set; }
}
