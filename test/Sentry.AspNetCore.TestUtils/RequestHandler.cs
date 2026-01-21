using Microsoft.AspNetCore.Http;

namespace Sentry.AspNetCore.TestUtils;

public class RequestHandler
{
    public string Path { get; set; }

    public Func<HttpContext, Task> Handler
    {
        get => field ?? (c => c.Response.WriteAsync(Response));
        set;
    }

    public string Response { get; set; }
}
