namespace Sentry.Internal.Extensions;

internal static class ReadOnlyDictionaryExtensions
{
    public static TValue? TryGetValue<TKey, TValue>(this IReadOnlyDictionary<TKey, object?> dictionary, TKey key)
        where TKey : notnull
        => dictionary.TryGetValue(key, out var value) && value is TValue typedValue
            ? typedValue
            : default;

    public static void AddIfNotNull<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue? value)
        where TKey : notnull
    {
        if (value is not null)
        {
            dictionary.Add(key, value);
        }
    }

    public static void AddIfNotNullOrEmpty<TKey>(this IDictionary<TKey, string> dictionary, TKey key, string? value)
        where TKey : notnull
    {
        if (!string.IsNullOrEmpty(value))
        {
            dictionary.Add(key, value);
        }
    }
}
