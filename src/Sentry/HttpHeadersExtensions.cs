using Sentry.Internal;
using Sentry.Internal.DataCollection;
using Sentry.Internal.Extensions;

namespace Sentry;

internal static class HttpHeadersExtensions
{
    internal static string? GetCookies(this HttpHeaders headers, KeyValueCollectionBehavior? behavior = null) =>
        headers.TryGetValues("Cookie", out var values)
            ? ToEventCookies(CookieFilter.FilterCookieHeaders(values, behavior))
            : null;

    internal static string? GetSetCookies(this HttpHeaders headers, KeyValueCollectionBehavior? behavior = null) =>
        headers.TryGetValues("Set-Cookie", out var values)
            ? ToEventCookies(CookieFilter.FilterSetCookieHeaders(values, behavior))
            : null;

    private static string? ToEventCookies(string[] cookies) =>
        cookies.Length > 0 ? CookieFilter.ToEventCookies(cookies) : null;

    internal static RedactedHeaders? ToRedactedHeaders(this Dictionary<string, string?>? headers)
    {
        var items = headers?.WhereNotNullValue();
        if (items is null || !items.Any())
        {
            return null;
        }
        return headers!;
    }
}
