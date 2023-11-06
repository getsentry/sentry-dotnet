namespace Sentry;

/// <summary>
/// Implemented by objects that contain a map of tags.
/// </summary>
public interface IHasTags
{
    /// <summary>
    /// Arbitrary key-value for this event.
    /// </summary>
    IDictionary<string, string> Tags { get; }
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
            hasTags.Tags[key] = value;
        }
    }

    /// <summary>
    /// Sets a tag on the object.
    /// </summary>
    /// <remarks>Included to make it easier to migrate from Sentry 3.x</remarks>
    public static void SetTag(this IHasTags? hasTags, string key, string value)
    {
        if (hasTags != null)
        {
            hasTags.Tags[key] = value;
        }
    }

    /// <summary>
    /// Removes a tag from the object.
    /// </summary>
    /// <remarks>Included to make it easier to migrate from Sentry 3.x</remarks>
    public static void UnsetTag(this IHasTags? hasTags, string key) => hasTags?.Tags.Remove(key);
}
