namespace Sentry.AspNet.Tests;

public static class HttpContextBuilder
{
    public static HttpContext Build(int responseStatusCode = 200)
    {
        return new HttpContext(
            new HttpRequest("test", "http://test/the/path", null),
            new HttpResponse(TextWriter.Null)
            {
                StatusCode = responseStatusCode
            })
        {
            ApplicationInstance = new HttpApplication()
        };
    }

    public static HttpContext BuildWithCookies(HttpCookie[] cookies, int responseStatusCode = 200)
    {
        var httpRequest = new HttpRequest("test", "http://test/the/path", null);
        foreach (var cookie in cookies)
        {
            httpRequest.Cookies.Add(cookie);
        }

        return new HttpContext(
            httpRequest,
            new HttpResponse(TextWriter.Null)
            {
                StatusCode = responseStatusCode
            })
        {
            ApplicationInstance = new HttpApplication()
        };
    }

    internal static readonly bool IsHttpHeaderMutationSupported = !RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    [System.Runtime.Versioning.UnsupportedOSPlatform("windows", "Works on Mono. But requires IIS7 on NetFx.")]
    public static HttpContext BuildWithHeaders(ReadOnlySpan<(string Key, string Value)> headers, int responseStatusCode = 200)
    {
        var httpRequest = new HttpRequest("test", "http://test/the/path", null);

        httpRequest.Headers.Unprotect();
        foreach (var header in headers)
        {
            httpRequest.Headers.Add(header.Key, header.Value);
        }
        httpRequest.Headers.Protect();

        return new HttpContext(
            httpRequest,
            new HttpResponse(TextWriter.Null)
            {
                StatusCode = responseStatusCode
            })
        {
            ApplicationInstance = new HttpApplication()
        };
    }
}

file static class NameValueCollectionExtensions
{
    private static readonly Type HeadersType = typeof(System.Collections.Specialized.NameValueCollection);
    private static readonly PropertyInfo IsReadOnlyProperty = HeadersType.GetProperty("IsReadOnly", BindingFlags.Instance | BindingFlags.NonPublic);

    extension(System.Collections.Specialized.NameValueCollection collection)
    {
        public void Protect()
        {
            IsReadOnlyProperty.SetValue(collection, true);
        }

        public void Unprotect()
        {
            IsReadOnlyProperty.SetValue(collection, false);
        }
    }
}
