namespace ClariveSDK.Models;

/// <summary>
/// Query parameters for <see cref="IClariveClient.ListEntriesAsync"/>.
/// </summary>
public class ListEntriesOptions
{
    /// <summary>Folder ID to filter by, or "all" for all folders.</summary>
    public string? FolderId { get; set; }

    /// <summary>Comma-separated tag names to filter by.</summary>
    public string? Tags { get; set; }

    /// <summary>Tag filter mode: "and" or "or".</summary>
    public string? TagMode { get; set; }

    /// <summary>Page number (1-based).</summary>
    public int? Page { get; set; }

    /// <summary>Items per page (max 100).</summary>
    public int? PageSize { get; set; }

    /// <summary>Search by title (case-insensitive).</summary>
    public string? Search { get; set; }

    /// <summary>Sort order: "recent", "alphabetical", or "oldest".</summary>
    public string? SortBy { get; set; }
}
