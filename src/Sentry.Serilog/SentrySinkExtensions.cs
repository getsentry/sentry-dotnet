// ReSharper disable once CheckNamespace - Discoverability

namespace Serilog;

/// <summary>
/// Sentry Serilog Sink extensions.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class SentrySinkExtensions
{
    private const string ObsoleteDsnOverload =
        "The Sentry sink no longer initializes the SDK, so a DSN can no longer be supplied to it. " +
        "Initialize Sentry with SentrySdk.Init (or an integration such as UseSentry), call UseSerilog() " +
        "on those options, and remove 'dsn' and any other core SDK settings from the sink configuration.";

    /// <summary>
    /// Not supported. The Sentry sink no longer initializes the SDK.
    /// </summary>
    /// <remarks>
    /// This overload only exists so that configuration providers that bind sink arguments by name (such as
    /// <c>Serilog.Settings.Configuration</c> reading <c>appsettings.json</c>) fail loudly instead of silently
    /// dropping a <c>dsn</c> that no longer has any effect. It always throws.
    /// </remarks>
    /// <param name="loggerConfiguration">The logger configuration. <seealso cref="LoggerSinkConfiguration"/></param>
    /// <param name="dsn">No longer supported. <seealso cref="SentryOptions.Dsn"/></param>
    /// <param name="minimumEventLevel">Minimum log level to send an event. <seealso cref="SentrySerilogOptions.MinimumEventLevel"/></param>
    /// <param name="minimumBreadcrumbLevel">Minimum log level to record a breadcrumb. <seealso cref="SentrySerilogOptions.MinimumBreadcrumbLevel"/></param>
    /// <param name="formatProvider">The Serilog format provider. <seealso cref="IFormatProvider"/></param>
    /// <param name="textFormatter">The Serilog text formatter. <seealso cref="ITextFormatter"/></param>
    /// <param name="restrictedToMinimumLevel">The minimum level for events passed through the sink. <seealso cref="SentrySerilogOptions.RestrictedToMinimumLevel"/></param>
    /// <param name="levelSwitch">A switch allowing the pass-through minimum level to be changed at runtime. <seealso cref="SentrySerilogOptions.LevelSwitch"/></param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always.</exception>
    [Obsolete(ObsoleteDsnOverload, error: true)]
    public static LoggerConfiguration Sentry(
        this LoggerSinkConfiguration loggerConfiguration,
        string dsn,
        LogEventLevel? minimumEventLevel = null,
        LogEventLevel? minimumBreadcrumbLevel = null,
        IFormatProvider? formatProvider = null,
        ITextFormatter? textFormatter = null,
        LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
        LoggingLevelSwitch? levelSwitch = null)
        => throw new NotSupportedException(ObsoleteDsnOverload);

    /// <summary>
    /// <para>Adds a Sentry Sink for Serilog.</para>
    /// <remarks>
    /// This doesn't initialise Sentry. Initialise Sentry separately, using <c>SentrySdk.Init</c> or another Sentry
    /// integration (such as ASP.NET Core or MAUI), and call <see cref="SentryOptionExtensions.UseSerilog"/> on the
    /// options used to do so.
    /// </remarks>
    /// </summary>
    /// <param name="loggerConfiguration">The logger configuration .<seealso cref="LoggerSinkConfiguration"/></param>
    /// <param name="minimumEventLevel">Minimum log level to send an event. <seealso cref="SentrySerilogOptions.MinimumEventLevel"/></param>
    /// <param name="minimumBreadcrumbLevel">Minimum log level to record a breadcrumb. <seealso cref="SentrySerilogOptions.MinimumBreadcrumbLevel"/></param>
    /// <param name="formatProvider">The Serilog format provider. <seealso cref="IFormatProvider"/></param>
    /// <param name="textFormatter">The Serilog text formatter. <seealso cref="ITextFormatter"/></param>
    /// <param name="restrictedToMinimumLevel">The minimum level for events passed through the sink. Ignored when <paramref name="levelSwitch"/> is specified. <seealso cref="SentrySerilogOptions.RestrictedToMinimumLevel"/></param>
    /// <param name="levelSwitch">A switch allowing the pass-through minimum level to be changed at runtime. <seealso cref="SentrySerilogOptions.LevelSwitch"/></param>
    /// <returns><see cref="LoggerConfiguration"/></returns>
    /// <example>This sample shows how each item may be set from within a configuration file:
    /// <code>
    /// {
    ///     "Serilog": {
    ///         "Using": [
    ///             "Serilog",
    ///             "Sentry",
    ///         ],
    ///         "WriteTo": [{
    ///                 "Name": "Sentry",
    ///                 "Args": {
    ///                     "minimumEventLevel": "Error",
    ///                     "minimumBreadcrumbLevel": "Verbose",
    ///                     "outputTemplate": "{Timestamp:o} [{Level:u3}] ({Application}/{MachineName}/{ThreadId}) {Message}{NewLine}{Exception}"
    ///                 }
    ///             }
    ///         ]
    ///     }
    /// }
    /// </code>
    /// </example>
    public static LoggerConfiguration Sentry(
        this LoggerSinkConfiguration loggerConfiguration,
        LogEventLevel? minimumEventLevel = null,
        LogEventLevel? minimumBreadcrumbLevel = null,
        IFormatProvider? formatProvider = null,
        ITextFormatter? textFormatter = null,
        LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
        LoggingLevelSwitch? levelSwitch = null)
    {
        return loggerConfiguration.Sentry(o => ConfigureSentrySerilogOptions(o,
            minimumEventLevel,
            minimumBreadcrumbLevel,
            formatProvider,
            textFormatter,
            restrictedToMinimumLevel,
            levelSwitch));
    }

    internal static void ConfigureSentrySerilogOptions(
        SentrySerilogOptions sentrySerilogOptions,
        LogEventLevel? minimumEventLevel = null,
        LogEventLevel? minimumBreadcrumbLevel = null,
        IFormatProvider? formatProvider = null,
        ITextFormatter? textFormatter = null,
        LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
        LoggingLevelSwitch? levelSwitch = null)
    {
        if (minimumEventLevel.HasValue)
        {
            sentrySerilogOptions.MinimumEventLevel = minimumEventLevel.Value;
        }

        if (minimumBreadcrumbLevel.HasValue)
        {
            sentrySerilogOptions.MinimumBreadcrumbLevel = minimumBreadcrumbLevel.Value;
        }

        if (formatProvider != null)
        {
            sentrySerilogOptions.FormatProvider = formatProvider;
        }

        if (textFormatter != null)
        {
            sentrySerilogOptions.TextFormatter = textFormatter;
        }

        sentrySerilogOptions.RestrictedToMinimumLevel = restrictedToMinimumLevel;
        sentrySerilogOptions.LevelSwitch = levelSwitch;
    }

    /// <summary>
    /// <para>Adds a Sentry Sink for Serilog.</para>
    /// <remarks>
    /// This doesn't initialise Sentry. Initialise Sentry separately, using <c>SentrySdk.Init</c> or another Sentry
    /// integration (such as ASP.NET Core or MAUI), and call <see cref="SentryOptionExtensions.UseSerilog"/> on the
    /// options used to do so.
    /// </remarks>
    /// </summary>
    /// <param name="loggerConfiguration">The logger configuration.</param>
    /// <param name="configureOptions">The configure options callback.</param>
    public static LoggerConfiguration Sentry(
        this LoggerSinkConfiguration loggerConfiguration,
        Action<SentrySerilogOptions> configureOptions)
    {
        var options = new SentrySerilogOptions();
        configureOptions?.Invoke(options);

        return loggerConfiguration.Sink(new SentrySink(options), options.RestrictedToMinimumLevel, options.LevelSwitch);
    }
}
