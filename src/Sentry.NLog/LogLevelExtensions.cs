using NLog;

using Sentry.Protocol;

namespace Sentry.NLog
{
    internal static class LogLevelExtensions
    {
        public static SentryLevel? ToSentryLevel(this LogLevel loggingLevel)
        {
            switch (loggingLevel?.Name)
            {
                case nameof(LogLevel.Debug):
                    return SentryLevel.Debug;

                case nameof(LogLevel.Error):
                    return SentryLevel.Error;

                case nameof(LogLevel.Fatal):
                    return SentryLevel.Fatal;

                case nameof(LogLevel.Info):
                    return SentryLevel.Info;

                case nameof(LogLevel.Trace):
                    return SentryLevel.Debug;

                case nameof(LogLevel.Warn):
                    return SentryLevel.Warning;

                default:
                    return null;
            }
        }

        public static BreadcrumbLevel ToBreadcrumbLevel(this LogLevel level)
        {
            switch (level.Name)
            {
                case nameof(LogLevel.Debug):
                    return BreadcrumbLevel.Debug;

                case nameof(LogLevel.Error):
                    return BreadcrumbLevel.Error;

                case nameof(LogLevel.Fatal):
                    return BreadcrumbLevel.Critical;

                case nameof(LogLevel.Info):
                    return BreadcrumbLevel.Info;

                case nameof(LogLevel.Trace):
                    return BreadcrumbLevel.Debug;

                case nameof(LogLevel.Warn):
                    return BreadcrumbLevel.Warning;

                default:
                    return BreadcrumbLevel.Info;
            }
        }
    }
}
