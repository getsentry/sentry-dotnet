using Microsoft.Extensions.Logging;

namespace Sentry.Extensions.Logging
{
    public class SentryLoggingOptions
    {
        public int MaxLogBreadcrumbs { get; set; } = 100;
        public LogLevel MinimumBreadcrumbLevel { get; set; } = LogLevel.Information;
        public LogLevel MinimumEventLevel { get; set; } = LogLevel.Error;
    }
}
