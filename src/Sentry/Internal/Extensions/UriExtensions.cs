namespace Sentry.Internal.Extensions;

internal static class UriExtensions
{
    public static string HttpRequestUrl(this Uri uri)
    {
        // This is a safe way to get the HttpRequestUrl, even when DisablePathAndQueryCanonicalization is true
        // See https://github.com/getsentry/sentry-dotnet/issues/3387
        return new UriBuilder(uri).Uri.GetComponents(UriComponents.HttpRequestUrl, UriFormat.Unescaped);
    }
}
