namespace Sentry;

/// <summary>
/// Implemented by objects that contain a map of tags.
/// </summary>
public interface IHasTags
{
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
/// Extensions for <see cref="IHasTags"/>.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class HasTagsExtensions
{
    /// <summary>
    /// Set all items as tags.
    /// </summary>
    public static void SetTags(this IHasTags hasTags, IEnumerable<KeyValuePair<string, string>> tags)
    {
        foreach (var (key, value) in tags)
        {
            hasTags.SetTag(key, value);
        }
    }

    /// <summary>
    /// Get tags or null.
    /// </summary>
    /// <param name="hasTags"></param>
    /// <returns></returns>
    public static IReadOnlyDictionary<string, string>? GetTagsOrNull(this IHasTags hasTags)
    {
        if (hasTags is SpanTracer s)
        {
            return s.InternalTags;
        }

        return hasTags.Tags;
    }
}
