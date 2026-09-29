namespace Sentry.Serilog;

/// <summary>
/// Options for the Sentry sink for Serilog.
/// </summary>
/// <remarks>
/// These options only configure the sink. The Sentry SDK itself is configured and initialised separately, using
/// <c>SentrySdk.Init</c> or another Sentry integration (such as ASP.NET Core or MAUI).
/// </remarks>
public class SentrySerilogOptions
{
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

    /// <summary>
    /// The minimum level for events passed through the sink. Ignored when <see cref="LevelSwitch"/> is specified.
    /// </summary>
    /// <seealso href="https://github.com/serilog/serilog/wiki/Configuration-Basics#overriding-per-sink"/>
    public LogEventLevel RestrictedToMinimumLevel { get; set; } = LevelAlias.Minimum;

    /// <summary>
    /// A switch allowing the pass-through minimum level to be changed at runtime.
    /// </summary>
    /// <seealso href="https://github.com/serilog/serilog/wiki/Configuration-Basics#overriding-per-sink"/>
    public LoggingLevelSwitch? LevelSwitch { get; set; }
}
