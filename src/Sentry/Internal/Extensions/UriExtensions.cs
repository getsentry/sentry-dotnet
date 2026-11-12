using Sentry.Internal.DataCollection;

namespace Sentry.Internal.Extensions;

internal static class UriExtensions
{
    public static string FilteredHttpRequestUrl(this Uri uri)
    {
        // This is a safe way to get the HttpRequestUrl, even when DisablePathAndQueryCanonicalization is true
        // See https://github.com/getsentry/sentry-dotnet/issues/3387
        var safeUri = new UriBuilder(uri).Uri;
        return safeUri.GetComponents(UriComponents.SchemeAndServer | UriComponents.Path, UriFormat.Unescaped)
            + QueryStringFilter.Filter(safeUri.Query);
    }
}
