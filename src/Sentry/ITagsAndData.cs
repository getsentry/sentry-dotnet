namespace Sentry;

/// <summary>
/// Implemented by objects that contain a map of untyped extra data.
/// </summary>
public interface ITagsAndData
{
    /// <summary>
    /// An arbitrary mapping of additional metadata to store with the event.
    /// </summary>
    IReadOnlyDictionary<string, object?> Extra { get; }

    /// <summary>
    /// Sets an extra.
    /// </summary>
    void SetExtra(string key, object? value);

    /// <summary>
    /// Arbitrary key-value for this event.
    /// </summary>
    IReadOnlyDictionary<string, string> Tags { get; }

    /// <summary>
    /// Sets a tag.
    /// </summary>
    void SetTag(string key, string value);

    /// <summary>
    /// Removes a tag.
    /// </summary>
    void UnsetTag(string key);
}

/// <summary>
/// Extensions for <see cref="ITagsAndData"/>.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class TagsAndDataExtensions
{
    /// <summary>
    /// Sets the extra key-value pairs to the object.
    /// </summary>
    public static void SetExtras(this ITagsAndData tagsAndData, IEnumerable<KeyValuePair<string, object?>> values)
    {
        foreach (var (key, value) in values)
        {
            tagsAndData.SetExtra(key, value);
        }
    }

    /// <summary>
    /// Set all items as tags.
    /// </summary>
    public static void SetTags(this ITagsAndData tagsAndData, IEnumerable<KeyValuePair<string, string>> tags)
    {
        foreach (var (key, value) in tags)
        {
            tagsAndData.SetTag(key, value);
        }
    }
}
