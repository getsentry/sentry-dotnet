namespace Sentry;

public partial class SentryOptions
{
    /// <summary>
    /// Enables native macOS crash reporting.
    /// </summary>
    public bool EnableNativeCrashReporting { get; set; } = true;
}
