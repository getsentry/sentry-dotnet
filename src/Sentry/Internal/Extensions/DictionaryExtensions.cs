namespace Sentry.Internal.Extensions;

internal static class DictionaryExtensions
{
    public static TValue? TryGetValue<TKey, TValue>(this IDictionary<TKey, object?> dictionary, TKey key)
        where TKey : notnull
        => dictionary.TryGetValue(key, out var value) && value is TValue typedValue
            ? typedValue
            : default;
}
