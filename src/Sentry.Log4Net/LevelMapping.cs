namespace Sentry.Log4Net;

internal static class LevelMapping
{
    public static SentryLevel? ToSentryLevel(this LoggingEvent loggingLevel)
    {
        return loggingLevel.Level switch
        {
            var l when l == Level.Fatal || l == Level.Emergency => SentryLevel.Fatal,
            var l when l == Level.Alert || l == Level.Critical || l == Level.Severe || l == Level.Error => SentryLevel.Error,
            var l when l == Level.Warn => SentryLevel.Warning,
            var l when l == Level.Notice || l == Level.Info => SentryLevel.Info,
            var l when l == Level.All || l == Level.Debug || l == Level.Verbose || l == Level.Trace || l == Level.Finer || l == Level.Finest ||
                       l == Level.Fine => SentryLevel.Debug,
            _ => null
        };
    }

    public static BreadcrumbLevel? ToBreadcrumbLevel(this LoggingEvent loggingLevel)
    {
        return loggingLevel.Level switch
        {
            var l when l == Level.Fatal || l == Level.Emergency => BreadcrumbLevel.Fatal,
            var l when l == Level.Alert || l == Level.Critical || l == Level.Severe || l == Level.Error => BreadcrumbLevel.Error,
            var l when l == Level.Warn => BreadcrumbLevel.Warning,
            var l when l == Level.Notice || l == Level.Info => BreadcrumbLevel.Info,
            var l when l == Level.Debug || l == Level.Verbose || l == Level.Trace || l == Level.Finer || l == Level.Finest ||
                l == Level.Fine || l == Level.All => BreadcrumbLevel.Debug,
            _ => null
        };
    }

    public static SentryLogLevel? ToSentryLogLevel(this LoggingEvent loggingEvent)
    {
        return loggingEvent.Level switch
        {
            var level when level == Level.Off => null,
            var level when level >= Level.Fatal => SentryLogLevel.Fatal,
            var level when level >= Level.Error => SentryLogLevel.Error,
            var level when level >= Level.Warn => SentryLogLevel.Warning,
            var level when level >= Level.Info => SentryLogLevel.Info,
            var level when level >= Level.Debug => SentryLogLevel.Debug,
            var level when level >= Level.Trace => SentryLogLevel.Trace,
            var level when level >= Level.All => SentryLogLevel.Trace,
            _ => null,
        };
    }
}
