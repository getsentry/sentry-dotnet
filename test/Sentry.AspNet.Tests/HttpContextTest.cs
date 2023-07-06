namespace Sentry.AspNet.Tests;

public abstract class HttpContextTest :
    IDisposable
{
    private HttpContext _context;

    protected HttpContextTest()
    {
        HttpContext.Current = Context = HttpContextBuilder.Build();
    }

    public HttpContext Context
    {
        get => _context;
        set => HttpContext.Current = _context = value;
    }

    public void Dispose()
    {
        HttpContext.Current = null;
    }
}
