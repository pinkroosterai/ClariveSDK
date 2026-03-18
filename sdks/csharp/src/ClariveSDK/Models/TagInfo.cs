namespace ClariveSDK.Models;

/// <summary>
/// A tag with its entry count, returned by <see cref="IClariveClient.ListTagsAsync"/>.
/// </summary>
/// <param name="Name">The tag name.</param>
/// <param name="EntryCount">Number of entries with this tag.</param>
public record TagInfo(
    string Name,
    int EntryCount);
