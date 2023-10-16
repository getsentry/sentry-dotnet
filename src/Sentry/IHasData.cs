namespace Sentry;

/// <summary>
/// Implemented by objects that contain a map of untyped extra data.
/// </summary>
public interface IHasData
{
    /// <summary>
    /// An arbitrary mapping of additional metadata to store with the event.
    /// </summary>
    IReadOnlyDictionary<string, object?> Data { get; }

    /// <summary>
    /// Sets a data value.
    /// </summary>
    void SetData(string key, object? value);
}

/// <summary>
/// Extensions for <see cref="IHasData"/>.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class HasExtraExtensions
{
    /// <summary>
    /// Sets the data key-value pairs on the object.
    /// </summary>
    public static void SetExtras(this IHasData hasData, IEnumerable<KeyValuePair<string, object?>> values)
    {
        foreach (var (key, value) in values)
        {
            hasData.SetData(key, value);
        }
    }
}
