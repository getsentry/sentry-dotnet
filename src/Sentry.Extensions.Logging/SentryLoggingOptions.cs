using Microsoft.Extensions.Logging;

namespace Sentry.Extensions.Logging;

/// <summary>
/// Sentry logging integration options
/// </summary>
/// <remarks>
/// These only configure which log entries are sent to Sentry. Sentry itself is initialized separately, with
/// <see cref="SentrySdk.Init(Action{SentryOptions})"/> or a framework integration such as <c>UseSentry</c>.
/// </remarks>
public class SentryLoggingOptions
{
    internal const string ObsoleteSdkInitialization =
        "The Microsoft.Extensions.Logging integration no longer initializes the SDK, so a DSN can no longer be " +
        "supplied to it. Initialize Sentry with UseSentry from Sentry.Extensions.Hosting (worker and other generic " +
        "host apps), Sentry.AspNetCore or Sentry.Maui, or with SentrySdk.Init. Then remove 'Dsn', 'InitializeSdk' " +
        "and any other core SDK settings from the logging configuration; UseSentry reads them from the 'Sentry' " +
        "section.";

    internal const string RedundantWithHostIntegration =
        "Logging.AddSentry() has no effect because the host integration, such as UseSentry, already adds Sentry's " +
        "logger providers. Its options are ignored: configure logging through the host's Sentry options.";

    internal const string OptionsIgnoredWithHostIntegration =
        "Logging.AddSentry() added back the logger providers that the host integration, such as UseSentry, adds. " +
        "They use the host's Sentry options, so the options passed to Logging.AddSentry() are ignored: configure " +
        "logging through the host's Sentry options.";

    internal const string HostLoggerProvidersRemoved =
        "Sentry's logger providers were removed after UseSentry added them, for example by ClearProviders(), so log " +
        "entries from ILogger don't reach Sentry. Call builder.Logging.AddSentry() after ClearProviders() to add them " +
        "back. If you send logs to Sentry through another integration, such as NLog, Serilog or log4net, you can " +
        "ignore this.";

    /// <summary>
    /// Not supported. The logging integration no longer initializes the SDK.
    /// </summary>
    /// <exception cref="NotSupportedException">When set.</exception>
    [Obsolete(ObsoleteSdkInitialization, error: true)]
    public string? Dsn
    {
        get => null;
        // Only a supplied value is an error: the configuration binder writes the getter's value back.
        set
        {
            if (value is not null)
            {
                throw new NotSupportedException(ObsoleteSdkInitialization);
            }
        }
    }

    /// <summary>
    /// Not supported. The logging integration no longer initializes the SDK.
    /// </summary>
    /// <exception cref="NotSupportedException">When set.</exception>
    [Obsolete(ObsoleteSdkInitialization, error: true)]
    public bool InitializeSdk
    {
        get => false;
        set
        {
            if (value)
            {
                throw new NotSupportedException(ObsoleteSdkInitialization);
            }
        }
    }

    /// <summary>
    /// Gets or sets the minimum breadcrumb level.
    /// </summary>
    /// <remarks>
    /// Events with this level or higher will be stored as <see cref="Breadcrumb"/>.
    /// </remarks>
    /// <value>
    /// The minimum breadcrumb level.
    /// </value>
    public LogLevel MinimumBreadcrumbLevel { get; set; } = LogLevel.Information;

    /// <summary>
    /// Gets or sets the minimum event level.
    /// </summary>
    /// <remarks>
    /// Events with this level or higher will be sent to Sentry.
    /// </remarks>
    /// <value>
    /// The minimum event level.
    /// </value>
    public LogLevel MinimumEventLevel { get; set; } = LogLevel.Error;

    /// <summary>
    /// Log entry filters
    /// </summary>
    internal ILogEntryFilter[] Filters { get; set; } = Array.Empty<ILogEntryFilter>();
}
