namespace Sentry.Internal.Extensions;

internal static class CollectionsExtensions
{
    public static TValue GetOrCreate<TValue>(
        this ConcurrentDictionary<string, object> dictionary,
        string key)
        where TValue : class, new()
    {
        var value = dictionary.GetOrAdd(key, _ => new TValue());

        if (value is TValue casted)
        {
            return casted;
        }

        throw new($"Expected a type of {typeof(TValue)} to exist for the key '{key}'. Instead found a {value.GetType()}. The likely cause of this is that the value for '{key}' has been incorrectly set to an instance of a different type.");
    }

    public static void TryCopyTo<TKey, TValue>(this IDictionary<TKey, TValue> from, IDictionary<TKey, TValue> to)
        where TKey : notnull
    {
        foreach (var (key, value) in from)
        {
            if (!to.ContainsKey(key))
            {
                to[key] = value;
            }
        }
    }

#if !NET8_0_OR_GREATER
    public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(
        this IEnumerable<KeyValuePair<TKey, TValue>> source) where TKey : notnull =>
        source.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
#endif

    public static IEnumerable<KeyValuePair<TKey, TValue>> WhereNotNullValue<TKey, TValue>(
        this IEnumerable<KeyValuePair<TKey, TValue?>> source) where TKey : notnull
    {
        foreach (var kvp in source)
        {
            if (kvp.Value is not null)
            {
                yield return kvp!;
            }
        }
    }

    public static IEnumerable<KeyValuePair<TKey, TValue>> Append<TKey, TValue>(
        this IEnumerable<KeyValuePair<TKey, TValue>> source, TKey key, TValue value) =>
        source.Append(new KeyValuePair<TKey, TValue>(key, value));

    public static IReadOnlyList<T> AsReadOnly<T>(this IList<T> list) =>
        list as IReadOnlyList<T> ?? new ReadOnlyCollection<T>(list);

#if !NET7_0_OR_GREATER
    public static IReadOnlyDictionary<TKey, TValue> AsReadOnly<TKey, TValue>(this IDictionary<TKey, TValue> dictionary)
        where TKey : notnull =>
        new ReadOnlyDictionary<TKey, TValue>(dictionary);
#endif

    public static IEnumerable<T> ExceptNulls<T>(this IEnumerable<T?> source) =>
        source.Where(x => x != null).Select(x => x!);

    public static bool TryGetTypedValue<T>(this IDictionary<string, object?> source, string key,
        [NotNullWhen(true)] out T value)
    {
        if (source.TryGetValue(key, out var obj) && obj is T typedValue)
        {
            value = typedValue;
            return true;
        }

        value = default!;
        return false;
    }
}
