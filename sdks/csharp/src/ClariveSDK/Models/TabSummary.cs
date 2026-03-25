namespace ClariveSDK.Models;

/// <summary>
/// Summary of a tab on a prompt entry.
/// </summary>
/// <param name="Id">The unique identifier of the tab.</param>
/// <param name="Name">The display name of the tab.</param>
/// <param name="IsMainTab">Whether this is the main (default) tab.</param>
/// <param name="ForkedFromVersion">The version number this tab was forked from, if any.</param>
public record TabSummary(
    Guid Id,
    string Name,
    bool IsMainTab,
    int? ForkedFromVersion);
