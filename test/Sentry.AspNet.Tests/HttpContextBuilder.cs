using System.Runtime.Versioning;

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

    public static HttpContext BuildWithHeaders(ReadOnlySpan<(string Key, string Value)> headers, int responseStatusCode = 200)
    {
        var httpRequest = new HttpRequest("test", "http://test/the/path", null);

#if WINDOWS
        SetHeaders(httpRequest, headers);
#else
        SetReadOnlyHeaders(httpRequest, headers);
#endif

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

    [SupportedOSPlatform("windows")]
    private static void SetHeaders(HttpRequest httpRequest, ReadOnlySpan<(string Key, string Value)> headers)
    {
        foreach (var header in headers)
        {
            httpRequest.Headers.Add(header.Key, header.Value);
        }
    }

    [UnsupportedOSPlatform("windows")]
    private static void SetReadOnlyHeaders(HttpRequest httpRequest, ReadOnlySpan<(string Key, string Value)> headers)
    {
        var httpRequestType = httpRequest.GetType();
        var setHeaderMethod = httpRequestType.GetMethod("SetHeader", BindingFlags.Instance | BindingFlags.NonPublic, null, [typeof(string), typeof(string)], null);
        var setHeaderParameters = new object[2];
        Assert.NotNull(setHeaderMethod);

        foreach (var header in headers)
        {
            setHeaderParameters[0] = header.Key;
            setHeaderParameters[1] = header.Value;
            setHeaderMethod.Invoke(httpRequest, setHeaderParameters);
        }
    }
}
