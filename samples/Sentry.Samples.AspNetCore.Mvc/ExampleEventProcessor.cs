using Sentry;
using Sentry.Extensibility;

namespace Samples.AspNetCore.Mvc;

public class ExampleEventProcessor : ISentryEventProcessor
{
    private readonly IHttpContextAccessor _httpContext;

    public ExampleEventProcessor(IHttpContextAccessor httpContext) => _httpContext = httpContext;

    public SentryEvent Process(SentryEvent @event)
    {
        // Here I can modify the event, while taking dependencies via DI

        @event.SetExtra("Response:HasStarted", _httpContext.HttpContext?.Response.HasStarted);
        return @event;
    }
}
