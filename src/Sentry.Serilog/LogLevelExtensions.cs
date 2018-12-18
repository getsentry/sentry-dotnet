using Sentry.Protocol;
using Serilog.Events;

namespace Sentry.Serilog
{
    internal static class LogLevelExtensions
    {
        public static SentryLevel? ToSentryLevel(this LogEventLevel loggingLevel)
        {
            switch (loggingLevel)
            {
                case LogEventLevel.Fatal:
                    return SentryLevel.Fatal;
                case LogEventLevel.Error:
                    return SentryLevel.Error;
                case LogEventLevel.Warning:
                    return SentryLevel.Warning;
                case LogEventLevel.Information:
                    return SentryLevel.Info;
                case LogEventLevel.Debug:
                    return SentryLevel.Debug;
            }

            return null;
        }

        public static BreadcrumbLevel ToBreadcrumbLevel(this LogEventLevel level)
        {
            switch (level)
            {
                case LogEventLevel.Verbose:
                case LogEventLevel.Debug:
                    return BreadcrumbLevel.Debug;
                case LogEventLevel.Information:
                    return BreadcrumbLevel.Info;
                case LogEventLevel.Warning:
                    return BreadcrumbLevel.Warning;
                case LogEventLevel.Error:
                    return BreadcrumbLevel.Error;
                case LogEventLevel.Fatal:
                    return BreadcrumbLevel.Critical;
                default:
                    return (BreadcrumbLevel)level;
            }
        }
    }
}
