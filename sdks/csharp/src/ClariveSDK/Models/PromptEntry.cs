namespace ClariveSDK.Models;

public record PromptEntry(
    Guid Id,
    string Title,
    string? SystemMessage,
    int Version,
    IReadOnlyList<Prompt> Prompts);
