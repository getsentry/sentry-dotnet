using Microsoft.Extensions.Logging;

namespace Sentry.Extensions.Logging;

/// <summary>
/// Sentry logging integration options
/// </summary>
/// <inheritdoc />
public class SentryLoggingOptions : SentryOptions
{
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
    /// Whether to initialize this SDK through this integration
    /// </summary>
    public bool InitializeSdk { get; set; } = true;

    /// <summary>
    /// Add a callback to configure the scope upon SDK initialization
    /// </summary>
    /// <param name="action">The function to invoke when initializing the SDK</param>
    public void ConfigureScope(Action<Scope> action) => ConfigureScopeCallbacks = ConfigureScopeCallbacks.Concat(new[] { action }).ToArray();

    /// <summary>
    /// Log entry filters
    /// </summary>
    internal ILogEntryFilter[] Filters { get; set; } = Array.Empty<ILogEntryFilter>();

    /// <summary>
    /// List of callbacks to be invoked when initializing the SDK
    /// </summary>
    internal Action<Scope>[] ConfigureScopeCallbacks { get; set; } = Array.Empty<Action<Scope>>();

    /// <summary>
    /// Experimental Sentry Logging features.
    /// </summary>
    /// <remarks>
    /// This and related experimental APIs may change in the future.
    /// </remarks>
    [Experimental(Infrastructure.DiagnosticId.ExperimentalFeature)]
    public SentryLoggingExperimentalOptions ExperimentalLogging { get; set; } = new();

    /// <summary>
    /// Experimental Sentry Logging options.
    /// </summary>
    /// <remarks>
    /// This and related experimental APIs may change in the future.
    /// </remarks>
    [Experimental(Infrastructure.DiagnosticId.ExperimentalFeature)]
    public sealed class SentryLoggingExperimentalOptions
    {
        internal SentryLoggingExperimentalOptions()
        {
        }

        /// <summary>
        /// Gets or sets the minimum log level.
        /// <para>This API is experimental and it may change in the future.</para>
        /// </summary>
        /// <remarks>
        /// Logs with this level or higher will be stored as <see cref="SentryLog"/>.
        /// </remarks>
        /// <value>
        /// The minimum log level.
        /// </value>
        public LogLevel MinimumLogLevel { get; set; } = LogLevel.Trace;
    }
}
