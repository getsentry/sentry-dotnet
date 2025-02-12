namespace Sentry;

/// <summary>
/// Implemented by objects that contain a map of untyped additional metadata.
/// </summary>
public interface IHasData
{
    /// <summary>
    /// An arbitrary mapping of additional metadata to store with the event.
    /// </summary>
    IReadOnlyDictionary<string, object?> Data { get; }

    /// <summary>
    /// Sets an extra.
    /// </summary>
    void SetData(string key, object? value);
}
