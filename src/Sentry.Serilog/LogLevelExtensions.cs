using Sentry.Protocol;
using Serilog.Events;

namespace Sentry.Serilog
{
    internal static class LogLevelExtensions
    {
        public static SentryLevel? ToSentryLevel(this LogEventLevel loggingLevel)
        {
            return loggingLevel switch
            {
                LogEventLevel.Fatal => SentryLevel.Fatal,
                LogEventLevel.Error => SentryLevel.Error,
                LogEventLevel.Warning => SentryLevel.Warning,
                LogEventLevel.Information => SentryLevel.Info,
                LogEventLevel.Debug => SentryLevel.Debug,
                LogEventLevel.Verbose => SentryLevel.Debug,
                _ => null
            };
        }

        public static BreadcrumbLevel ToBreadcrumbLevel(this LogEventLevel level)
        {
            return level switch
            {
                LogEventLevel.Verbose => BreadcrumbLevel.Debug,
                LogEventLevel.Debug => BreadcrumbLevel.Debug,
                LogEventLevel.Information => BreadcrumbLevel.Info,
                LogEventLevel.Warning => BreadcrumbLevel.Warning,
                LogEventLevel.Error => BreadcrumbLevel.Error,
                LogEventLevel.Fatal => BreadcrumbLevel.Critical,
                _ => (BreadcrumbLevel)level
            };
        }
    }
}
