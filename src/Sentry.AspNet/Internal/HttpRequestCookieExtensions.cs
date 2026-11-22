using Sentry.Internal.DataCollection;

namespace Sentry.AspNet.Internal;

internal static class HttpRequestCookieExtensions
{
    internal static string[] GetFilteredCookies(this HttpRequest request)
    {
        // Unvalidated, because Request.Cookies throws HttpRequestValidationException for values like "<script>"
        var cookies = request.Unvalidated.Cookies;
        var pairs = new KeyValuePair<string?, string?>[cookies.Count];
        for (var i = 0; i < cookies.Count; i++)
        {
            pairs[i] = new KeyValuePair<string?, string?>(cookies[i].Name, cookies[i].Value);
        }

        return CookieFilter.FilterCookiePairs(pairs);
    }
}
