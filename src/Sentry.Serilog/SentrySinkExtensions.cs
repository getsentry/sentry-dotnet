// ReSharper disable once CheckNamespace - Discoverability

namespace Serilog;

/// <summary>
/// Sentry Serilog Sink extensions.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class SentrySinkExtensions
{
    /// <summary>
    /// Initialize Sentry and add the SentrySink for Serilog.
    /// </summary>
    /// <param name="loggerConfiguration">The logger configuration .<seealso cref="LoggerSinkConfiguration"/></param>
    /// <param name="dsn">The Sentry DSN (required). <seealso cref="SentryOptions.Dsn"/></param>
    /// <param name="minimumEventLevel">Minimum log level to send an event. <seealso cref="SentrySerilogOptions.MinimumEventLevel"/></param>
    /// <param name="minimumBreadcrumbLevel">Minimum log level to record a breadcrumb. <seealso cref="SentrySerilogOptions.MinimumBreadcrumbLevel"/></param>
    /// <param name="formatProvider">The Serilog format provider. <seealso cref="IFormatProvider"/></param>
    /// <param name="textFormatter">The Serilog text formatter. <seealso cref="ITextFormatter"/></param>
    /// <param name="sendDefaultPii">Whether to include default Personal Identifiable information. <seealso cref="SentryOptions.SendDefaultPii"/></param>
    /// <param name="isEnvironmentUser">Whether to report the <see cref="System.Environment.UserName"/> as the User affected in the event. <seealso cref="SentryOptions.IsEnvironmentUser"/></param>
    /// <param name="serverName">Gets or sets the name of the server running the application. <seealso cref="SentryOptions.ServerName"/></param>
    /// <param name="attachStackTrace">Whether to send the stack trace of a event captured without an exception. <seealso cref="SentryOptions.AttachStacktrace"/></param>
    /// <param name="maxBreadcrumbs">Gets or sets the maximum breadcrumbs. <seealso cref="SentryOptions.MaxBreadcrumbs"/></param>
    /// <param name="sampleRate">The rate to sample events. <seealso cref="SentryOptions.SampleRate"/></param>
    /// <param name="release">The release version of the application. <seealso cref="SentryOptions.Release"/></param>
    /// <param name="environment">The environment the application is running. <seealso cref="SentryOptions.Environment"/></param>
    /// <param name="maxQueueItems">The maximum number of events to keep while the worker attempts to send them. <seealso cref="SentryOptions.MaxQueueItems"/></param>
    /// <param name="shutdownTimeout">How long to wait for events to be sent before shutdown. <seealso cref="SentryOptions.ShutdownTimeout"/></param>
    /// <param name="decompressionMethods">Decompression methods accepted. <seealso cref="SentryOptions.DecompressionMethods"/></param>
    /// <param name="requestBodyCompressionLevel">The level of which to compress the <see cref="SentryEvent"/> before sending to Sentry. <seealso cref="SentryOptions.RequestBodyCompressionLevel"/></param>
    /// <param name="requestBodyCompressionBuffered">Whether the body compression is buffered and the request 'Content-Length' known in advance. <seealso cref="SentryOptions.RequestBodyCompressionBuffered"/></param>
    /// <param name="debug">Whether to log diagnostics messages. <seealso cref="SentryOptions.Debug"/></param>
    /// <param name="diagnosticLevel">The diagnostics level to be used. <seealso cref="SentryOptions.DiagnosticLevel"/></param>
    /// <param name="reportAssembliesMode">What mode to use for reporting referenced assemblies in each event sent to sentry. Defaults to <see cref="Sentry.ReportAssembliesMode.Version"/></param>
    /// <param name="deduplicateMode">What modes to use for event automatic de-duplication. <seealso cref="SentryOptions.DeduplicateMode"/></param>
    /// <param name="defaultTags">Defaults tags to add to all events. <seealso cref="SentryOptions.DefaultTags"/></param>
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
    ///                     "dsn": "https://MY-DSN@sentry.io",
    ///                     "minimumBreadcrumbLevel": "Verbose",
    ///                     "minimumEventLevel": "Error",
    ///                     "outputTemplate": "{Timestamp:o} [{Level:u3}] ({Application}/{MachineName}/{ThreadId}) {Message}{NewLine}{Exception}"///
    ///                     "sendDefaultPii": false,
    ///                     "isEnvironmentUser": false,
    ///                     "serverName": "MyServerName",
    ///                     "attachStackTrace": false,
    ///                     "maxBreadcrumbs": 20,
    ///                     "sampleRate": 0.5,
    ///                     "release": "0.0.1",
    ///                     "environment": "staging",
    ///                     "maxQueueItems": 100,
    ///                     "shutdownTimeout": "00:00:05",
    ///                     "decompressionMethods": "GZip",
    ///                     "requestBodyCompressionLevel": "NoCompression",
    ///                     "requestBodyCompressionBuffered": false,
    ///                     "debug": false,
    ///                     "diagnosticLevel": "Debug",
    ///                     "reportAssembliesMode": ReportAssembliesMode.None,
    ///                     "deduplicateMode": "All",
    ///                     "defaultTags": {
    ///                         "key-1", "value-1",
    ///                         "key-2", "value-2"
    ///                     }
    ///                 }
    ///             }
    ///         ]
    ///     }
    /// }
    /// </code>
    /// </example>
    public static LoggerConfiguration Sentry(
        this LoggerSinkConfiguration loggerConfiguration,
        string dsn,
        LogEventLevel? minimumBreadcrumbLevel = null,
        LogEventLevel? minimumEventLevel = null,
        IFormatProvider? formatProvider = null,
        ITextFormatter? textFormatter = null,
        bool? sendDefaultPii = null,
        bool? isEnvironmentUser = null,
        string? serverName = null,
        bool? attachStackTrace = null,
        int? maxBreadcrumbs = null,
        float? sampleRate = null,
        string? release = null,
        string? environment = null,
        int? maxQueueItems = null,
        TimeSpan? shutdownTimeout = null,
        DecompressionMethods? decompressionMethods = null,
        CompressionLevel? requestBodyCompressionLevel = null,
        bool? requestBodyCompressionBuffered = null,
        bool? debug = null,
        SentryLevel? diagnosticLevel = null,
        ReportAssembliesMode? reportAssembliesMode = null,
        DeduplicateMode? deduplicateMode = null,
        Dictionary<string, string>? defaultTags = null)
    {
        return loggerConfiguration.Sentry(o => ConfigureSentrySerilogOptions(o,
            dsn,
            minimumEventLevel,
            minimumBreadcrumbLevel,
            formatProvider,
            textFormatter,
            sendDefaultPii,
            isEnvironmentUser,
            serverName,
            attachStackTrace,
            maxBreadcrumbs,
            sampleRate,
            release,
            environment,
            maxQueueItems,
            shutdownTimeout,
            decompressionMethods,
            requestBodyCompressionLevel,
            requestBodyCompressionBuffered,
            debug,
            diagnosticLevel,
            reportAssembliesMode,
            deduplicateMode,
            defaultTags));
    }

    /// <summary>
    /// <para>Adds a Sentry Sink for Serilog.</para>
    /// <remarks>
    /// Note this overload doesn't initialize Sentry for you, so you'll need to have already done so. Alternatively you
    /// can use use the overload of this extension method, passing a DSN string in the first argument.
    /// </remarks>
    /// </summary>
    /// <param name="loggerConfiguration">The logger configuration .<seealso cref="LoggerSinkConfiguration"/></param>
    /// <param name="minimumEventLevel">Minimum log level to send an event. <seealso cref="SentrySerilogOptions.MinimumEventLevel"/></param>
    /// <param name="minimumBreadcrumbLevel">Minimum log level to record a breadcrumb. <seealso cref="SentrySerilogOptions.MinimumBreadcrumbLevel"/></param>
    /// <param name="formatProvider">The Serilog format provider. <seealso cref="IFormatProvider"/></param>
    /// <param name="textFormatter">The Serilog text formatter. <seealso cref="ITextFormatter"/></param>
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
    ///                     "outputTemplate": "{Timestamp:o} [{Level:u3}] ({Application}/{MachineName}/{ThreadId}) {Message}{NewLine}{Exception}"///
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
        ITextFormatter? textFormatter = null
        )
    {
        return loggerConfiguration.Sentry(o => ConfigureSentrySerilogOptions(o,
            null,
            minimumEventLevel,
            minimumBreadcrumbLevel,
            formatProvider,
            textFormatter));
    }

    internal static void ConfigureSentrySerilogOptions(
        SentrySerilogOptions sentrySerilogOptions,
        string? dsn,
        LogEventLevel? minimumEventLevel = null,
        LogEventLevel? minimumBreadcrumbLevel = null,
        IFormatProvider? formatProvider = null,
        ITextFormatter? textFormatter = null,
        bool? sendDefaultPii = null,
        bool? isEnvironmentUser = null,
        string? serverName = null,
        bool? attachStackTrace = null,
        int? maxBreadcrumbs = null,
        float? sampleRate = null,
        string? release = null,
        string? environment = null,
        int? maxQueueItems = null,
        TimeSpan? shutdownTimeout = null,
        DecompressionMethods? decompressionMethods = null,
        CompressionLevel? requestBodyCompressionLevel = null,
        bool? requestBodyCompressionBuffered = null,
        bool? debug = null,
        SentryLevel? diagnosticLevel = null,
        ReportAssembliesMode? reportAssembliesMode = null,
        DeduplicateMode? deduplicateMode = null,
        Dictionary<string, string>? defaultTags = null)
    {
        sentrySerilogOptions.Dsn = dsn ?? sentrySerilogOptions.Dsn;
        sentrySerilogOptions.MinimumEventLevel = minimumEventLevel ?? sentrySerilogOptions.MinimumEventLevel;
        sentrySerilogOptions.MinimumBreadcrumbLevel = minimumBreadcrumbLevel ?? sentrySerilogOptions.MinimumBreadcrumbLevel;
        sentrySerilogOptions.FormatProvider = formatProvider ?? sentrySerilogOptions.FormatProvider;
        sentrySerilogOptions.TextFormatter = textFormatter ?? sentrySerilogOptions.TextFormatter;
        sentrySerilogOptions.SendDefaultPii = sendDefaultPii ?? sentrySerilogOptions.SendDefaultPii;
        sentrySerilogOptions.IsEnvironmentUser = isEnvironmentUser ?? sentrySerilogOptions.IsEnvironmentUser;
        if (!string.IsNullOrWhiteSpace(serverName))
        {
            sentrySerilogOptions.ServerName = serverName;
        }
        sentrySerilogOptions.AttachStacktrace = attachStackTrace ?? sentrySerilogOptions.AttachStacktrace;
        sentrySerilogOptions.MaxBreadcrumbs = maxBreadcrumbs ?? sentrySerilogOptions.MaxBreadcrumbs;
        sentrySerilogOptions.SampleRate = sampleRate ?? sentrySerilogOptions.SampleRate;
        if (!string.IsNullOrWhiteSpace(release))
        {
            sentrySerilogOptions.Release = release;
        }
        if (!string.IsNullOrWhiteSpace(environment))
        {
            sentrySerilogOptions.Environment = environment;
        }
        sentrySerilogOptions.MaxQueueItems = maxQueueItems ?? sentrySerilogOptions.MaxQueueItems;
        sentrySerilogOptions.ShutdownTimeout = shutdownTimeout ?? sentrySerilogOptions.ShutdownTimeout;
        sentrySerilogOptions.DecompressionMethods = decompressionMethods ?? sentrySerilogOptions.DecompressionMethods;
        sentrySerilogOptions.RequestBodyCompressionLevel = requestBodyCompressionLevel ?? sentrySerilogOptions.RequestBodyCompressionLevel;
        sentrySerilogOptions.RequestBodyCompressionBuffered = requestBodyCompressionBuffered ?? sentrySerilogOptions.RequestBodyCompressionBuffered;
        sentrySerilogOptions.Debug = debug ?? sentrySerilogOptions.Debug;
        sentrySerilogOptions.DiagnosticLevel = diagnosticLevel ?? sentrySerilogOptions.DiagnosticLevel;
        sentrySerilogOptions.ReportAssembliesMode = reportAssembliesMode ?? sentrySerilogOptions.ReportAssembliesMode;
        sentrySerilogOptions.DeduplicateMode = deduplicateMode ?? sentrySerilogOptions.DeduplicateMode;

        // Serilog-specific items
        sentrySerilogOptions.InitializeSdk = dsn is not null;
        if (defaultTags?.Count > 0)
        {
            foreach (var tag in defaultTags)
            {
                sentrySerilogOptions.DefaultTags.Add(tag.Key, tag.Value);
            }
        }
    }

    /// <summary>
    /// Add Sentry sink to Serilog.
    /// </summary>
    /// <param name="loggerConfiguration">The logger configuration.</param>
    /// <param name="configureOptions">The configure options callback.</param>
    public static LoggerConfiguration Sentry(
        this LoggerSinkConfiguration loggerConfiguration,
        Action<SentrySerilogOptions> configureOptions)
    {
        var options = new SentrySerilogOptions();
        configureOptions?.Invoke(options);

        IDisposable? sdkDisposable = null;
        if (options.InitializeSdk)
        {
            sdkDisposable = SentrySdk.Init(options);
        }

        var minimumOverall = (LogEventLevel)Math.Min((int)options.MinimumBreadcrumbLevel, (int)options.MinimumEventLevel);
        return loggerConfiguration.Sink(new SentrySink(options, sdkDisposable), minimumOverall);
    }
}
