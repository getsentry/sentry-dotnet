using Sentry.Internal;
using Sentry.Internal.Extensions;

namespace Sentry;

internal static class HttpHeadersExtensions
{
    internal static string GetCookies(this HttpHeaders headers) =>
        headers.TryGetValues("Cookie", out var values)
            ? string.Join("; ", values)
            : string.Empty;

    internal static RedactedHeaders? Redact(this Dictionary<string, string?>? headers)
    {
        var items = headers?.WhereNotNullValue();
        if (items is null || !items.Any())
        {
            return null;
        }
        return headers!;
    }
}
