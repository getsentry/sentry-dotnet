#nullable enable

namespace Sentry.AspNet.Tests;

public abstract class HttpContextTest : IDisposable
{
    protected HttpContextTest()
    {
        HttpContext.Current = Context = HttpContextBuilder.Build();
    }

    public HttpContext Context
    {
        get;
        set => HttpContext.Current = field = value;
    }

    public void Dispose()
    {
        HttpContext.Current = null;
    }
}
