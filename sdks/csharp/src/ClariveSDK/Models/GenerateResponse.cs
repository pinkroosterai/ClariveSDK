namespace ClariveSDK.Models;

/// <summary>
/// Response from <see cref="IClariveClient.GenerateAsync"/> containing rendered prompts.
/// </summary>
/// <param name="Id">The unique identifier of the entry.</param>
/// <param name="Title">The display name of the entry.</param>
/// <param name="Version">The published version number.</param>
/// <param name="SystemMessage">The system message with all template variables substituted.</param>
/// <param name="RenderedPrompts">The prompts with all template variables replaced by the provided values.</param>
public record GenerateResponse(
    Guid Id,
    string Title,
    int Version,
    string? SystemMessage,
    IReadOnlyList<RenderedPrompt> RenderedPrompts);
