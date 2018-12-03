using System;
using Serilog;
using Serilog.Configuration;
using Serilog.Events;

namespace Sentry.Serilog
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
