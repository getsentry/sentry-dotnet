using Microsoft.Extensions.Logging;

namespace Sentry.Extensions.Logging;

/// <inheritdoc cref="BindableSentryOptions"/>
internal class BindableSentryHostOptions : BindableSentryOptions
{
    public LogLevel? MinimumBreadcrumbLevel { get; set; }
    public LogLevel? MinimumEventLevel { get; set; }

    public void ApplyTo(SentryHostOptions options)
    {
        base.ApplyTo(options);
        options.MinimumBreadcrumbLevel = MinimumBreadcrumbLevel ?? options.MinimumBreadcrumbLevel;
        options.MinimumEventLevel = MinimumEventLevel ?? options.MinimumEventLevel;
    }
}
