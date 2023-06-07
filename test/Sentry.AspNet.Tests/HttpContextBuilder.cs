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

}
