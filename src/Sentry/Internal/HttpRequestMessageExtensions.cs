namespace Sentry.Internal;

internal static class HttpRequestMessageExtensions
{
    public static string MethodString(this HttpRequestMessage? request) => request?.Method.Method.ToUpperInvariant() ?? string.Empty;
    public static string UrlString(this HttpRequestMessage? request) => request?.RequestUri?.ToString() ?? string.Empty;
}
