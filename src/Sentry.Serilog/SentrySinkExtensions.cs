using System;
using System.Reflection;
using Sentry;
using Sentry.Serilog;
using Serilog.Configuration;
using Serilog.Events;

// ReSharper disable once CheckNamespace - Discoverability
namespace Serilog
{
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
        /// <param name="environment">The environment the event occured in.</param>
        /// <param name="release">The release version the event occured in.</param>
        /// <returns></returns>
        public static LoggerConfiguration Sentry(
            this LoggerSinkConfiguration loggerConfiguration,
            string dsn = null,
            LogEventLevel minimumBreadcrumbLevel = LogEventLevel.Information,
            LogEventLevel minimumEventLevel = LogEventLevel.Error,
            IFormatProvider formatProvider = null,
            string environment = null,
            string release = null)
            => loggerConfiguration.Sentry(o =>
                {
                    if (dsn != null)
                    {
                        o.Dsn = new Dsn(dsn);
                    }
                    o.MinimumBreadcrumbLevel = minimumBreadcrumbLevel;
                    o.MinimumEventLevel = minimumEventLevel;
                    o.FormatProvider = formatProvider;

                    var environmentToUse = string.IsNullOrWhiteSpace(release)
                        ? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT",
                              EnvironmentVariableTarget.Machine) ??
                          Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT",
                              EnvironmentVariableTarget.Process)
                        : environment;

                    var releaseToUse = string.IsNullOrWhiteSpace(release)
                        ? (Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly()).GetName().Version?.ToString()
                        : release;

                    if (environmentToUse != null)
                    {
                        o.Environment = environmentToUse;
                    }

                    if (releaseToUse != null)
                    {
                        o.Release = releaseToUse;
                    }
                });

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
