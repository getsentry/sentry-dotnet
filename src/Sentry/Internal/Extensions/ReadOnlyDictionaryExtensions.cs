namespace Sentry.Internal.Extensions;

internal static class ReadOnlyDictionaryExtensions
{
    public static TValue? TryGetValue<TKey, TValue>(this IReadOnlyDictionary<TKey, object?> dictionary, TKey key)
        where TKey : notnull
        => dictionary.TryGetValue(key, out var value) && value is TValue typedValue
            ? typedValue
            : default;
}
