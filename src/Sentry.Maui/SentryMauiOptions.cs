using Sentry.Extensions.Logging;

namespace Sentry.Maui;

/// <summary>
/// Sentry MAUI integration options
/// </summary>
public class SentryMauiOptions : SentryLoggingOptions
{
    /// <summary>
    /// Creates a new instance of <see cref="SentryMauiOptions"/>.
    /// </summary>
    public SentryMauiOptions()
    {
        // Set defaults for options that are different for MAUI.
        // The user can change these. If you want to force a value, use SentryMauiOptionsSetup instead.
        // Also, some of these are already set in the base Sentry SDK, but since we don't yet have native targets
        // there for all MAUI targets, we'll set them again here.

        AutoSessionTracking = true;
        DetectStartupTime = StartupTimeDetectionMode.Fast;
        LogCatIntegration = LogCatIntegrationType.Unhandled;
#if !PLATFORM_NEUTRAL
        CacheDirectoryPath = Microsoft.Maui.Storage.FileSystem.CacheDirectory;
#endif
    }

    /// <summary>
    /// Gets or sets whether elements that implement <see cref="IText"/>
    /// (such as <see cref="Button"/>, <see cref="Label"/>, <see cref="Entry"/>, and others)
    /// will have their text included on breadcrumbs.
    /// Use caution when enabling, as such values may contain personally identifiable information (PII).
    /// The default is <c>false</c> (exclude).
    /// </summary>
    public bool IncludeTextInBreadcrumbs { get; set; }

    /// <summary>
    /// Gets or sets whether elements that implement <see cref="ITitledElement"/>
    /// (such as <see cref="Window"/>, <see cref="Page"/>, and others)
    /// will have their titles included on breadcrumbs.
    /// Use caution when enabling, as such values may contain personally identifiable information (PII).
    /// The default is <c>false</c> (exclude).
    /// </summary>
    public bool IncludeTitleInBreadcrumbs { get; set; }

    /// <summary>
    /// Gets or sets whether the breadcrumb sent for the <see cref="Window.Backgrounding"/>
    /// event will include state data from <see cref="BackgroundingEventArgs.State"/>.
    /// Use caution when enabling, as such values may contain personally identifiable information (PII).
    /// The default is <c>false</c> (exclude).
    /// </summary>
    public bool IncludeBackgroundingStateInBreadcrumbs { get; set; }

    /// <summary>
    /// Gets or sets whether when LogCat logs are attached to events.
    /// The default is <see cref="LogCatIntegrationType.Unhandled"/>
    /// </summary>
    public LogCatIntegrationType LogCatIntegration { get; set; }
}
