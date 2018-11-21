using Sentry.Protocol;
using Serilog.Events;

namespace Sentry.Serilog
{
    internal static class LevelMapping
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
    }
}
