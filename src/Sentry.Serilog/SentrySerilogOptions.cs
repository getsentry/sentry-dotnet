namespace Sentry.Serilog;

/// <summary>
/// Sentry Options for Serilog logging
/// </summary>
/// <inheritdoc />
public class SentrySerilogOptions : SentryOptions
{
    /// <summary>
    /// Whether to initialize this SDK through this integration
    /// </summary>
    public bool InitializeSdk { get; set; } = true;

    /// <summary>
    /// Minimum log level to send an event.
    /// </summary>
    /// <remarks>
    /// Events with this level or higher will be sent to Sentry.
    /// </remarks>
    /// <value>
    /// The minimum event level.
    /// </value>
    public LogEventLevel MinimumEventLevel { get; set; } = LogEventLevel.Error;

    /// <summary>
    /// Minimum log level to record a breadcrumb.
    /// </summary>
    /// <remarks>Events with this level or higher will be stored as <see cref="Breadcrumb"/></remarks>
    /// <value>
    /// The minimum breadcrumb level.
    /// </value>
    public LogEventLevel MinimumBreadcrumbLevel { get; set; } = LogEventLevel.Information;

    /// <summary>
    /// Optional <see cref="IFormatProvider"/>
    /// </summary>
    public IFormatProvider? FormatProvider { get; set; }

    /// <summary>
    /// Optional <see cref="ITextFormatter"/>
    /// </summary>
    public ITextFormatter? TextFormatter { get; set; }
}
