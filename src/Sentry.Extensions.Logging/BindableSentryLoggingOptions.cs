using Microsoft.Extensions.Logging;

namespace Sentry.Extensions.Logging;

internal class BindableSentryLoggingOptions
{
    public LogLevel? MinimumBreadcrumbLevel { get; set; }
    public LogLevel? MinimumEventLevel { get; set; }
    public string? Dsn { get; set; }
    public bool? InitializeSdk { get; set; }

    public void ApplyTo(SentryLoggingOptions options)
    {
        if (Dsn is not null || InitializeSdk == true)
        {
            throw new NotSupportedException(SentryLoggingOptions.ObsoleteSdkInitialization);
        }

        options.MinimumBreadcrumbLevel = MinimumBreadcrumbLevel ?? options.MinimumBreadcrumbLevel;
        options.MinimumEventLevel = MinimumEventLevel ?? options.MinimumEventLevel;
    }
}
