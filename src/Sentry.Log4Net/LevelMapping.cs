using log4net.Core;
using Sentry.Protocol;

namespace Sentry.Log4Net
{
    internal static class LevelMapping
    {
        public static SentryLevel? ToSentryLevel(this LoggingEvent loggingLevel)
        {
            switch (loggingLevel.Level)
            {
                case var l when l == Level.Fatal
                                || l == Level.Emergency
                                || l == Level.All:
                    return SentryLevel.Fatal;
                case var l when l == Level.Alert
                                || l == Level.Critical
                                || l == Level.Severe
                                || l == Level.Error:
                    return SentryLevel.Error;
                case var l when l == Level.Warn:
                    return SentryLevel.Warning;
                case var l when l == Level.Notice
                                || l == Level.Info:
                    return SentryLevel.Info;
                case var l when l == Level.Debug
                                || l == Level.Verbose
                                || l == Level.Trace
                                || l == Level.Finer
                                || l == Level.Finest
                                || l == Level.Fine:
                    return SentryLevel.Debug;
            }

            return null;
        }
    }
}
