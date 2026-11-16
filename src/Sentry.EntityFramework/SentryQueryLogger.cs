namespace Sentry.EntityFramework;

internal class SentryQueryLogger : IQueryLogger
{
    private readonly SentryOptions _options;
    private readonly IHub _hub;

    public SentryQueryLogger(SentryOptions options, IHub? hub = null)
    {
        _options = options;
        _hub = hub ?? HubAdapter.Instance;
    }

    public void Log(string text, BreadcrumbLevel level = BreadcrumbLevel.Debug)
    {
        if (!_options.DataCollectionResolver.DatabaseQueryData)
        {
            return;
        }

        _hub.AddBreadcrumb(
            message: text,
            category: "Entity Framework",
            type: null,
            data: null,
            level: level);
    }
}
