using Microsoft.Extensions.Logging;
using Sentry.Protocol;

namespace Sentry.Extensions.Logging
{
    internal static class LogLevelExtensions
    {
        public static BreadcrumbLevel ToBreadcrumbLevel(this LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Trace:
                    return BreadcrumbLevel.Debug;
                case LogLevel.Debug:
                    return BreadcrumbLevel.Debug;
                case LogLevel.Information:
                    return BreadcrumbLevel.Info;
                case LogLevel.Warning:
                    return BreadcrumbLevel.Warning;
                case LogLevel.Error:
                    return BreadcrumbLevel.Error;
                case LogLevel.Critical:
                    return BreadcrumbLevel.Critical;
                case LogLevel.None:
                default:
                    return (BreadcrumbLevel)level;
            }
        }

        public static LogLevel ToMicrosoft(this SentryLevel level)
        {
            switch (level)
            {
                case SentryLevel.Debug:
                    return LogLevel.Debug;
                case SentryLevel.Info:
                    return LogLevel.Information;
                case SentryLevel.Warning:
                    return LogLevel.Warning;
                case SentryLevel.Error:
                    return LogLevel.Error;
                case SentryLevel.Fatal:
                    return LogLevel.Critical;
                default:
                    return LogLevel.Debug;
            }
        }

        public static SentryLevel ToSentryLevel(this LogLevel level)
        {
            switch (level)
            {
                case LogLevel.None:
                case LogLevel.Trace:
                case LogLevel.Debug:
                    return SentryLevel.Debug;
                case LogLevel.Information:
                    return SentryLevel.Info;
                case LogLevel.Warning:
                    return SentryLevel.Warning;
                case LogLevel.Error:
                    return SentryLevel.Error;
                case LogLevel.Critical:
                    return SentryLevel.Fatal;
                default:
                    return SentryLevel.Debug;
            }
        }
    }
}
