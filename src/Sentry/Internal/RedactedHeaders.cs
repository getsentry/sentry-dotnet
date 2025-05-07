using Sentry.Internal.Extensions;

namespace Sentry.Internal;

internal class RedactedHeaders : IDictionary<string, string>
{
    private static readonly string[] SensitiveKeys = ["Authorization", "Proxy-Authorization"];

    private readonly Dictionary<string, string> _inner = new(StringComparer.OrdinalIgnoreCase);

    private static string Redact(string key, string value) =>
        SensitiveKeys.Contains(key, StringComparer.OrdinalIgnoreCase) ? "[Filtered]" : value;

    public string this[string key]
    {
        get => _inner[key];
        set => _inner[key] = Redact(key, value);
    }

    public void Add(string key, string value) => _inner.Add(key, Redact(key, value));

    // Delegate rest to _inner
    public bool ContainsKey(string key) => _inner.ContainsKey(key);
    public bool Remove(string key) => _inner.Remove(key);
#if NET8_0_OR_GREATER
    public bool TryGetValue(string key, [MaybeNullWhen(false)] out string value) => _inner.TryGetValue(key, out value);
#else
    public bool TryGetValue(string key, out string value) => _inner.TryGetValue(key, out value);
#endif
    public ICollection<string> Keys => _inner.Keys;
    public ICollection<string> Values => _inner.Values;
    public int Count => _inner.Count;
    public bool IsReadOnly => false;

    public void Add(KeyValuePair<string, string> item) => Add(item.Key, item.Value);
    public void Clear() => _inner.Clear();
    public bool Contains(KeyValuePair<string, string> item) => _inner.Contains(item);
    public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex) =>
        ((IDictionary<string, string>)_inner).CopyTo(array, arrayIndex);
    public bool Remove(KeyValuePair<string, string> item) => _inner.Remove(item.Key);
    public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => _inner.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => _inner.GetEnumerator();

    public static implicit operator RedactedHeaders(Dictionary<string, string> source)
    {
        var result = new RedactedHeaders();
        foreach (var kvp in source)
        {
            result[kvp.Key] = kvp.Value; // This will sanitize
        }
        return result;
    }
}
