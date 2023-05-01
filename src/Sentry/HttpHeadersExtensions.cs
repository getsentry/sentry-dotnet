namespace Sentry;

internal static class HttpHeadersExtensions
{
    internal static string GetCookies(this HttpHeaders headers) =>
        headers.TryGetValues("Cookie", out var values)
            ? string.Join("; ", values)
            : string.Empty;
}
