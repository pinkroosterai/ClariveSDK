namespace ClariveSDK.Models;

/// <summary>
/// Compact representation of a published entry, returned by <see cref="IClariveClient.ListEntriesAsync"/>.
/// </summary>
/// <param name="Id">The unique identifier of the entry.</param>
/// <param name="Title">The display name of the entry.</param>
/// <param name="Version">The published version number.</param>
/// <param name="HasSystemMessage">Whether the entry has a system message.</param>
/// <param name="IsTemplate">Whether the entry contains template variables.</param>
/// <param name="IsChain">Whether the entry has multiple prompts (a chain).</param>
/// <param name="PromptCount">The number of prompts in the entry.</param>
/// <param name="FirstPromptPreview">Preview text from the first prompt.</param>
/// <param name="Tags">Tags assigned to the entry.</param>
/// <param name="CreatedAt">When the entry was created.</param>
/// <param name="UpdatedAt">When the entry was last updated.</param>
/// <param name="Tabs">Tab summaries for this entry.</param>
/// <param name="TabCount">The number of tabs on this entry.</param>
public record EntrySummary(
    Guid Id,
    string Title,
    int Version,
    bool HasSystemMessage,
    bool IsTemplate,
    bool IsChain,
    int PromptCount,
    string? FirstPromptPreview,
    IReadOnlyList<string> Tags,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IReadOnlyList<TabSummary> Tabs,
    int TabCount);
