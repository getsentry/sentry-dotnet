using Microsoft.Extensions.Logging;

namespace Sentry.Extensions.Logging;

/// <inheritdoc cref="BindableSentryOptions"/>
internal class BindableSentryLoggingOptions : BindableSentryOptions
{
    public LogLevel? MinimumBreadcrumbLevel { get; set; }
    public LogLevel? MinimumEventLevel { get; set; }
    public bool? InitializeSdk { get; set; }

    public void ApplyTo(SentryLoggingOptions options)
    {
        base.ApplyTo(options);
        options.MinimumBreadcrumbLevel = MinimumBreadcrumbLevel ?? options.MinimumBreadcrumbLevel;
        options.MinimumEventLevel = MinimumEventLevel ?? options.MinimumEventLevel;
        options.InitializeSdk = InitializeSdk?? options.InitializeSdk;
    }
}
