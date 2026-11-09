using Sentry.Internal.DataCollection;

namespace Sentry.AspNet.Internal;

internal static class HttpRequestCookieExtensions
{
    internal static string[] GetFilteredCookies(this HttpRequest request)
    {
        // Unvalidated, because Request.Cookies throws HttpRequestValidationException for values like "<script>"
        var cookies = request.Unvalidated.Cookies;
        return CookieFilter.FilterCookiePairs(
            cookies.AllKeys.Select(key => new KeyValuePair<string?, string?>(key, cookies[key]?.Value)));
    }
}
