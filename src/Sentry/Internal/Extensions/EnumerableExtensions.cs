namespace Sentry.Internal.Extensions;

internal static class EnumerableExtensions
{
    public static IEnumerable<KeyValuePair<string, string>> WithValues(this IEnumerable<KeyValuePair<string, string?>> items) =>
        items.Where(kvp => kvp.Value != null)
             .Select(kvp => new KeyValuePair<string, string>(kvp.Key, kvp.Value!));
}
