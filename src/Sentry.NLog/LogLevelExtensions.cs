using NLog;

using Sentry.Protocol;

namespace Sentry.NLog
{
    internal static class LogLevelExtensions
    {
        public static SentryLevel? ToSentryLevel(this LogLevel loggingLevel)
        {
            return loggingLevel.Name switch
            {
                nameof(LogLevel.Debug) => SentryLevel.Debug,
                nameof(LogLevel.Error) => SentryLevel.Error,
                nameof(LogLevel.Fatal) => SentryLevel.Fatal,
                nameof(LogLevel.Info) => SentryLevel.Info,
                nameof(LogLevel.Trace) => SentryLevel.Debug,
                nameof(LogLevel.Warn) => SentryLevel.Warning,
                _ => null
            };
        }

        public static BreadcrumbLevel ToBreadcrumbLevel(this LogLevel level)
        {
            return level.Name switch
            {
                nameof(LogLevel.Debug) => BreadcrumbLevel.Debug,
                nameof(LogLevel.Error) => BreadcrumbLevel.Error,
                nameof(LogLevel.Fatal) => BreadcrumbLevel.Critical,
                nameof(LogLevel.Info) => BreadcrumbLevel.Info,
                nameof(LogLevel.Trace) => BreadcrumbLevel.Debug,
                nameof(LogLevel.Warn) => BreadcrumbLevel.Warning,
                _ => BreadcrumbLevel.Info
            };
        }
    }
}
