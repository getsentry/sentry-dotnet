namespace Sentry.Log4Net;

internal static class LevelMapping
{
    public static SentryLevel? ToSentryLevel(this LoggingEvent loggingLevel)
    {
        return loggingLevel.Level switch
        {
            var l when l == Level.Fatal || l == Level.Emergency || l == Level.All => SentryLevel.Fatal,
            var l when l == Level.Alert || l == Level.Critical || l == Level.Severe || l == Level.Error => SentryLevel.Error,
            var l when l == Level.Warn => SentryLevel.Warning,
            var l when l == Level.Notice || l == Level.Info => SentryLevel.Info,
            var l when l == Level.Debug || l == Level.Verbose || l == Level.Trace || l == Level.Finer || l == Level.Finest ||
                       l == Level.Fine => SentryLevel.Debug,
            _ => null
        };
    }

    public static BreadcrumbLevel? ToBreadcrumbLevel(this LoggingEvent loggingLevel)
    {
        return loggingLevel.Level switch
        {
            var l when l == Level.Fatal || l == Level.Emergency || l == Level.All => BreadcrumbLevel.Critical,
            var l when l == Level.Alert || l == Level.Critical || l == Level.Severe || l == Level.Error => BreadcrumbLevel.Error,
            var l when l == Level.Warn => BreadcrumbLevel.Warning,
            var l when l == Level.Notice || l == Level.Info => BreadcrumbLevel.Info,
            var l when l == Level.Debug || l == Level.Verbose || l == Level.Trace || l == Level.Finer || l == Level.Finest ||
                       l == Level.Fine => BreadcrumbLevel.Debug,
            _ => null
        };
    }
}
