namespace ClariveSDK.Models;

public record GenerateResponse(
    Guid Id,
    string Title,
    int Version,
    string? SystemMessage,
    IReadOnlyList<RenderedPrompt> RenderedPrompts);
