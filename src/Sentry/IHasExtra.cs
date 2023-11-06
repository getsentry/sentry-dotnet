namespace Sentry;

/// <summary>
/// Implemented by objects that contain a map of untyped extra data.
/// </summary>
public interface IHasExtra
{
    /// <summary>
    /// An arbitrary mapping of additional metadata to store with the event.
    /// </summary>
    IDictionary<string, object?> Extra { get; }
}

/// <summary>
/// Extensions for <see cref="IHasExtra"/>.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class HasExtraExtensions
{
    /// <summary>
    /// Sets the extra key-value pairs to the object.
    /// </summary>
    public static void SetExtras(this IHasExtra hasExtra, IEnumerable<KeyValuePair<string, object?>> values)
    {
        foreach (var (key, value) in values)
        {
            hasExtra.Extra[key] = value;
        }
    }

    /// <summary>
    /// Sets extra data on the object.
    /// </summary>
    /// <remarks>Included to make it easier to migrate from Sentry 3.x</remarks>
    public static void SetExtra(this IHasExtra? hasExtra, string key, object? value)
    {
        if (hasExtra != null)
        {
            hasExtra.Extra[key] = value;
        }
    }
}
