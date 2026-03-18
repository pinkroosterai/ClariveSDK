namespace ClariveSDK.Models;

/// <summary>
/// A prompt with all template variables substituted, returned by <see cref="IClariveClient.GenerateAsync"/>.
/// </summary>
/// <param name="Content">The fully rendered prompt text.</param>
/// <param name="Order">The display/execution order.</param>
public record RenderedPrompt(
    string Content,
    int Order);
