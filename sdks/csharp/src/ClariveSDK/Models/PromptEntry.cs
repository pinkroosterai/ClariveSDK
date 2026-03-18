namespace ClariveSDK.Models;

/// <summary>
/// A published prompt entry retrieved from the Clarive API.
/// </summary>
/// <param name="Id">The unique identifier of the entry.</param>
/// <param name="Title">The display name of the entry.</param>
/// <param name="SystemMessage">Optional system message for the LLM. May contain <c>{{variable}}</c> placeholders.</param>
/// <param name="Version">The published version number.</param>
/// <param name="Prompts">The ordered list of prompts in this entry.</param>
public record PromptEntry(
    Guid Id,
    string Title,
    string? SystemMessage,
    int Version,
    IReadOnlyList<Prompt> Prompts);
