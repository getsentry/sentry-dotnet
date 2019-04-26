using System.Collections.Generic;

using NLog;

using Sentry.Protocol;

namespace Sentry.NLog
{
    internal static class LogLevelExtensions
    {
        private static readonly IDictionary<LogLevel, SentryLevel> SentryLevelMap = new Dictionary<LogLevel, SentryLevel>
        {
            { LogLevel.Debug, SentryLevel.Debug },
            { LogLevel.Error, SentryLevel.Error },
            { LogLevel.Fatal, SentryLevel.Fatal },
            { LogLevel.Info,  SentryLevel.Info },
            { LogLevel.Trace, SentryLevel.Debug },
            { LogLevel.Warn,  SentryLevel.Warning },
        };

        public static SentryLevel? ToSentryLevel(this LogLevel loggingLevel)
        {
            if (SentryLevelMap.TryGetValue(loggingLevel, out SentryLevel level))
                return level;

            return null;
        }

        private static readonly IDictionary<LogLevel, BreadcrumbLevel> BreadcrumbLevelMap = new Dictionary<LogLevel, BreadcrumbLevel>
        {
            { LogLevel.Debug, BreadcrumbLevel.Debug },
            { LogLevel.Error, BreadcrumbLevel.Error },
            { LogLevel.Fatal, BreadcrumbLevel.Critical },
            { LogLevel.Info, BreadcrumbLevel.Info },
            { LogLevel.Trace, BreadcrumbLevel.Debug },
            { LogLevel.Warn, BreadcrumbLevel.Warning },
        };

        public static BreadcrumbLevel ToBreadcrumbLevel(this LogLevel level)
        {
            return BreadcrumbLevelMap[level];
        }
    }
}