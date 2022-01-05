using System.Web;

public static class HttpContextBuilder
{
    public static HttpContext Build(int responseStatusCode = 200)
    {
        return new HttpContext(new HttpRequest("test", "http://test/the/path", null), new HttpResponse(TextWriter.Null)
        {
            StatusCode = responseStatusCode
        });
    }
}
