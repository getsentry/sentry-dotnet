using System.Web;

public static class HttpContextBuilder
{
    public static HttpContext Build(int responseStatusCode = 200)
    {
        return new(
            new("test", "http://test/the/path", null),
            new(TextWriter.Null)
            {
                StatusCode = responseStatusCode
            })
        {
            ApplicationInstance = new()
        };
    }
}
