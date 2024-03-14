namespace Sentry.Internal.Extensions;

internal static class DictionaryExtensions
{
    public static void AddIfNotNullOrEmpty<TKey>(this IDictionary<TKey, string> dictionary, TKey key, string? value)
        where TKey : notnull
    {
        if (!string.IsNullOrEmpty(value))
        {
            dictionary.Add(key, value);
        }
    }
}
