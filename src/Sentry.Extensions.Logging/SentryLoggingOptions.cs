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
