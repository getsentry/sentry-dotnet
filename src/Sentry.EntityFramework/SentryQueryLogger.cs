using Sentry.Internal.DataCollection;

namespace Sentry.EntityFramework;

internal class SentryQueryLogger : IQueryLogger
{
    private readonly IHub _hub;

    public SentryQueryLogger(IHub? hub = null) => _hub = hub ?? HubAdapter.Instance;

    public void Log(string text, BreadcrumbLevel level = BreadcrumbLevel.Debug)
        => Log(text, SqlDialect.Generic, level);

    internal void Log(string text, SqlDialect dialect, BreadcrumbLevel level)
        => _hub.AddBreadcrumb(
            message: _hub.GetSentryOptions()?.DataCollectionResolver.SanitizeDbStatements == true
                ? SqlSanitizer.Sanitize(text, dialect)
                : text,
            category: "Entity Framework",
            type: null,
            data: null,
            level: level);
}
