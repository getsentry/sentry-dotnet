using System;
using System.IO.Compression;
using System.Net;
using Sentry;
using Sentry.Protocol;
using Sentry.Serilog;
using Serilog.Configuration;
using Serilog.Events;
using Constants = Sentry.Protocol.Constants;

// ReSharper disable once CheckNamespace - Discoverability
namespace Serilog
{
    /// <summary>
    /// Sentry Serilog Sink extensions.
    /// </summary>
    public static class SentrySinkExtensions
    {
        /// <summary>
        /// Add Sentry Serilog Sink.
        /// </summary>
        /// <param name="loggerConfiguration">The logger configuration.</param>
        /// <param name="dsn">The Sentry DSN.</param>
        /// <param name="minimumBreadcrumbLevel">Minimum log level to record a breadcrumb.</param>
        /// <param name="minimumEventLevel">Minimum log level to send an event.</param>
        /// <param name="formatProvider">The Serilog format provider.</param>
        /// <returns></returns>
        public static LoggerConfiguration Sentry(
            this LoggerSinkConfiguration loggerConfiguration,
            string dsn = null,
            LogEventLevel minimumBreadcrumbLevel = LogEventLevel.Information,
            LogEventLevel minimumEventLevel = LogEventLevel.Error,
            IFormatProvider formatProvider = null)
            => loggerConfiguration.Sentry(o =>
            {
                if (dsn != null)
                {
                    o.Dsn = new Dsn(dsn);
                }
                o.MinimumBreadcrumbLevel = minimumBreadcrumbLevel;
                o.MinimumEventLevel = minimumEventLevel;
                o.FormatProvider = formatProvider;
            });

        /// <summary>
        /// Add Sentry Serilog Sink.
        /// </summary>
        /// <param name="loggerConfiguration">The logger configuration .<seealso cref="LoggerSinkConfiguration"/></param>
        /// <param name="sendDefaultPii">Whether to include default Personal Identifiable information. <seealso cref="SentryOptions.SendDefaultPii"/></param>
        /// <param name="isEnvironmentUser">Whether to report the <see cref="System.Environment.UserName"/> as the User affected in the event. <seealso cref="SentryOptions.IsEnvironmentUser"/></param>
        /// <param name="serverName">Gets or sets the name of the server running the application. <seealso cref="SentryOptions.ServerName"/></param>
        /// <param name="attachStackTrace">Whether to send the stack trace of a event captured without an exception. <seealso cref="SentryOptions.AttachStacktrace"/></param>
        /// <param name="maxBreadcrumbs">Gets or sets the maximum breadcrumbs. <seealso cref="SentryOptions.MaxBreadcrumbs"/></param>
        /// <param name="sampleRate">The rate to sample events. <seealso cref="SentryOptions.SampleRate"/></param>
        /// <param name="release">The release version of the application. <seealso cref="SentryOptions.Release"/></param>
        /// <param name="environment">The environment the application is running. <seealso cref="SentryOptions.Environment"/></param>
        /// <param name="dsn">The Sentry DSN. <seealso cref="SentryOptions.Dsn"/></param>
        /// <param name="maxQueueItems">The maximum number of events to keep while the worker attempts to send them. <seealso cref="SentryOptions.MaxQueueItems"/></param>
        /// <param name="shutdownTimeout">How long to wait for events to be sent before shutdown. <seealso cref="SentryOptions.ShutdownTimeout"/></param>
        /// <param name="decompressionMethods">Decompression methods accepted. <seealso cref="SentryOptions.DecompressionMethods"/></param>
        /// <param name="requestBodyCompressionLevel">The level of which to compress the <see cref="SentryEvent"/> before sending to Sentry. <seealso cref="SentryOptions.RequestBodyCompressionLevel"/></param>
        /// <param name="requestBodyCompressionBuffered">Whether the body compression is buffered and the request 'Content-Length' known in advance. <seealso cref="SentryOptions.RequestBodyCompressionBuffered"/></param>
        /// <param name="debug">Whether to log diagnostics messages. <seealso cref="SentryOptions.Debug"/></param>
        /// <param name="diagnosticsLevel">The diagnostics level to be used. <seealso cref="SentryOptions.DiagnosticsLevel"/></param>
        /// <param name="reportAssemblies">Whether or not to include referenced assemblies in each event sent to sentry. Defaults to <see langword="true"/>. <seealso cref="SentryOptions.ReportAssemblies"/></param>
        /// <param name="deduplicateMode">What modes to use for event automatic deduplication. <seealso cref="SentryOptions.DeduplicateMode"/></param>
        /// <param name="initializeSdk">Whether to initialize this SDK through this integration. <seealso cref="SentrySerilogOptions.InitializeSdk"/></param>
        /// <param name="minimumEventLevel">Minimum log level to send an event. <seealso cref="SentrySerilogOptions.MinimumEventLevel"/></param>
        /// <param name="minimumBreadcrumbLevel">Minimum log level to record a breadcrumb. <seealso cref="SentrySerilogOptions.MinimumBreadcrumbLevel"/></param>
        /// <param name="formatProvider">The Serilog format provider. <seealso cref="IFormatProvider"/></param>
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
        ///                     "sendDefaultPii": false,
        ///                     "isEnvironmentUser": false,
        ///                     "serverName": "MyServerName"
        ///                     "attachStackTrace": false,
        ///                     "maxBreadcrumbs": 20
        ///                     "sampleRate": 0.5,
        ///                     "release": "0.0.1",
        ///                     "environment": "staging",
        ///                     "dsn": "https://MY-DSN@sentry.io",
        ///                     "maxQueueItems": 100,
        ///                     "shutdownTimeout": "00:00:05",
        ///                     "decompressionMethods": "GZip",
        ///                     "requestBodyCompressionLevel": "NoCompression",
        ///                     "requestBodyCompressionBuffered": false,
        ///                     "debug": false,
        ///                     "diagnosticsLevel": "Debug",
        ///                     "reportAssemblies": false
        ///                     "deduplicateMode": "All",
        ///                     "initializeSdk": true,
        ///                     "minimumBreadcrumbLevel": "Verbose",
        ///                     "minimumEventLevel": "Error",
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
            bool sendDefaultPii = false,
            bool isEnvironmentUser = true,
            string serverName = null,
            bool attachStackTrace = false,
            int maxBreadcrumbs = Constants.DefaultMaxBreadcrumbs,
            float? sampleRate = null,
            string release = null,
            string environment = null,
            string dsn = null,
            int maxQueueItems = 30,
            TimeSpan shutdownTimeout = default,
            DecompressionMethods decompressionMethods = ~DecompressionMethods.None,
            CompressionLevel requestBodyCompressionLevel = CompressionLevel.Optimal,
            bool requestBodyCompressionBuffered = true,
            bool debug = false,
            SentryLevel diagnosticsLevel = SentryLevel.Debug,
            bool reportAssemblies = true,
            DeduplicateMode deduplicateMode = DeduplicateMode.All ^ DeduplicateMode.InnerException,
            bool initializeSdk = true,
            LogEventLevel minimumEventLevel = LogEventLevel.Error,
            LogEventLevel minimumBreadcrumbLevel = LogEventLevel.Information,
            IFormatProvider formatProvider = null)
            => loggerConfiguration.Sentry(o =>
            {
                o.SendDefaultPii = sendDefaultPii;
                o.IsEnvironmentUser = isEnvironmentUser;
                o.ServerName = serverName;
                o.AttachStacktrace = attachStackTrace;
                o.MaxBreadcrumbs = maxBreadcrumbs;

                if (sampleRate != null)
                {
                    o.SampleRate = sampleRate;
                }

                o.Release = release;
                o.Environment = environment;

                if (dsn != null)
                {
                    o.Dsn = new Dsn(dsn);
                }

                o.MaxQueueItems = maxQueueItems;

                if (shutdownTimeout != default)
                {
                    o.ShutdownTimeout = shutdownTimeout;
                }

                o.DecompressionMethods = decompressionMethods;
                o.RequestBodyCompressionLevel = requestBodyCompressionLevel;
                o.RequestBodyCompressionBuffered = requestBodyCompressionBuffered;
                o.Debug = debug;
                o.DiagnosticsLevel = diagnosticsLevel;
                o.ReportAssemblies = reportAssemblies;
                o.DeduplicateMode = deduplicateMode;

                // Serilog-specific items
                o.InitializeSdk = initializeSdk;
                o.MinimumEventLevel = minimumEventLevel;
                o.MinimumBreadcrumbLevel = minimumBreadcrumbLevel;
                o.FormatProvider = formatProvider;
            });

        /// <summary>
        /// Add Sentry sink to Serilog.
        /// </summary>
        /// <param name="loggerConfiguration">The logger configuration.</param>
        /// <param name="configureOptions">The configure options callback.</param>
        /// <returns></returns>
        public static LoggerConfiguration Sentry(
            this LoggerSinkConfiguration loggerConfiguration,
            Action<SentrySerilogOptions> configureOptions)
        {
            var options = new SentrySerilogOptions();
            configureOptions?.Invoke(options);

            IDisposable sdkDisposable = null;
            if (options.InitializeSdk)
            {
                sdkDisposable = SentrySdk.Init(options);
            }

            var minimumOverall = (LogEventLevel)Math.Min((int)options.MinimumBreadcrumbLevel, (int)options.MinimumEventLevel);
            return loggerConfiguration.Sink(new SentrySink(options, sdkDisposable), minimumOverall);
        }
    }
}
