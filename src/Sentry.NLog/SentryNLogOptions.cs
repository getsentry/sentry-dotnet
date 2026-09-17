namespace Sentry.NLog;

/// <summary>
/// Options for the Sentry target for NLog. All properties can be configured via code or in NLog.config xml file.
/// </summary>
/// <remarks>
/// These options only configure the target. The Sentry SDK itself is configured and initialised separately, using
/// <c>SentrySdk.Init</c> or another Sentry integration (such as ASP.NET Core or MAUI).
/// </remarks>
[NLogConfigurationItem]
public class SentryNLogOptions
{
    /// <summary>
    /// How long to wait for Sentry to flush when NLog is flushed. Defaults to 15 seconds, the same as NLog.
    /// </summary>
    public TimeSpan FlushTimeout { get; set; } = TimeSpan.FromSeconds(15);

    /// <summary>
    /// Minimum log level for events to trigger a send to Sentry. Defaults to <see cref="M:LogLevel.Error" />.
    /// </summary>
    public LogLevel? MinimumEventLevel { get; set; } = LogLevel.Error;

    /// <summary>
    /// Minimum log level to be included in the breadcrumb. Defaults to <see cref="M:LogLevel.Info" />.
    /// </summary>
    public LogLevel? MinimumBreadcrumbLevel { get; set; } = LogLevel.Info;

    /// <summary>
    /// Set this to <see langword="true" /> to ignore log messages that don't contain an exception.
    /// </summary>
    public bool IgnoreEventsWithNoException { get; set; } = false;

    /// <summary>
    /// Determines whether event properties will be sent to sentry as Tags or not. Defaults to <see langword="false" />.
    /// </summary>
    public bool IncludeEventPropertiesAsTags { get; set; } = false;

    /// <summary>
    /// Determines whether or not to include event-level data as data in breadcrumbs for future errors.
    /// Defaults to <see langword="false" />.
    /// </summary>
    public bool IncludeEventDataOnBreadcrumbs { get; set; } = false;

    /// <summary>
    /// Custom layout for breadcrumbs.
    /// </summary>
    [NLogConfigurationIgnoreProperty] // Configure this directly on the target in XML config.
    public Layout? BreadcrumbLayout { get; set; }

    /// <summary>
    /// Custom layout for breadcrumbs category
    /// </summary>
    [NLogConfigurationIgnoreProperty] // Configure this directly on the target in XML config.
    public Layout? BreadcrumbCategoryLayout { get; set; }

    /// <summary>
    /// Configured layout for rendering SentryEvent message
    /// </summary>
    [NLogConfigurationIgnoreProperty] // Configure this directly on the target in XML config.
    public Layout? Layout { get; set; }

    /// <summary>
    /// Any additional tags to apply to each logged message.
    /// </summary>
    [NLogConfigurationIgnoreProperty] // Configure this directly on the target in XML config.
    public IList<TargetPropertyWithContext> Tags { get; } = new List<TargetPropertyWithContext>();

    /// <summary>
    /// Optionally configure one or more parts of the user information to be rendered dynamically from an NLog layout
    /// </summary>
    [NLogConfigurationIgnoreProperty] // Configure this directly on the target in XML config.
    public SentryNLogUser? User { get; set; } = new();
}
