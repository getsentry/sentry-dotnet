namespace Sentry;

/// <summary>
/// Implemented by objects that contain a map of tags.
/// </summary>
public interface IHasTags
{
    /// <summary>
    /// Arbitrary key-value for this event.
    /// </summary>
    public IReadOnlyDictionary<string, string> Tags { get; }

    /// <summary>
    /// Sets a tag.
    /// </summary>
    public void SetTag(string key, string value);

    /// <summary>
    /// Removes a tag.
    /// </summary>
    public void UnsetTag(string key);
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
}
