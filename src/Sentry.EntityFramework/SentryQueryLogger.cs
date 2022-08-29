namespace Sentry.EntityFramework;

internal class SentryQueryLogger : IQueryLogger
{
    private readonly IHub _hub;

    public SentryQueryLogger(IHub? hub = null) => _hub = hub ?? HubAdapter.Instance;

    public void Log(string text, BreadcrumbLevel level = BreadcrumbLevel.Debug)
        => _hub.AddBreadcrumb(
            message: text,
            category: "Entity Framework",
            type: null,
            data: null,
            level: level);
}
