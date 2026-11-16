using Sentry.Internal;
using Sentry.Internal.DataCollection;
using Sentry.Internal.Extensions;

namespace Sentry;

internal static class HttpHeadersExtensions
{
    internal static string GetCookies(this HttpHeaders headers, KeyValueCollectionBehavior? behavior = null) =>
        headers.TryGetValues("Cookie", out var values)
            ? CookieFilter.ToEventCookies(CookieFilter.FilterCookieHeaders(values, behavior))
            : string.Empty;

    internal static string GetSetCookies(this HttpHeaders headers, KeyValueCollectionBehavior? behavior = null) =>
        headers.TryGetValues("Set-Cookie", out var values)
            ? CookieFilter.ToEventCookies(CookieFilter.FilterSetCookieHeaders(values, behavior))
            : string.Empty;

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
