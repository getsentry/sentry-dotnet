using Microsoft.Extensions.Logging;

namespace Sentry.Extensions.Logging;

internal static class LogLevelExtensions
{
    public static BreadcrumbLevel ToBreadcrumbLevel(this LogLevel level)
    {
        return level switch
        {
            LogLevel.Trace => BreadcrumbLevel.Debug,
            LogLevel.Debug => BreadcrumbLevel.Debug,
            LogLevel.Information => BreadcrumbLevel.Info,
            LogLevel.Warning => BreadcrumbLevel.Warning,
            LogLevel.Error => BreadcrumbLevel.Error,
            LogLevel.Critical => BreadcrumbLevel.Critical,
            _ => (BreadcrumbLevel)level
        };
    }

    public static LogLevel ToMicrosoft(this SentryLevel level)
    {
        return level switch
        {
            SentryLevel.Debug => LogLevel.Debug,
            SentryLevel.Info => LogLevel.Information,
            SentryLevel.Warning => LogLevel.Warning,
            SentryLevel.Error => LogLevel.Error,
            SentryLevel.Fatal => LogLevel.Critical,
            _ => LogLevel.Debug
        };
    }

    public static SentryLevel ToSentryLevel(this LogLevel level)
    {
        return level switch
        {
            LogLevel.None => SentryLevel.Debug,
            LogLevel.Trace => SentryLevel.Debug,
            LogLevel.Debug => SentryLevel.Debug,
            LogLevel.Information => SentryLevel.Info,
            LogLevel.Warning => SentryLevel.Warning,
            LogLevel.Error => SentryLevel.Error,
            LogLevel.Critical => SentryLevel.Fatal,
            _ => SentryLevel.Debug
        };
    }

    public static SentryLogLevel ToSentryLogLevel(this LogLevel logLevel)
    {
        return logLevel switch
        {
            LogLevel.Trace => SentryLogLevel.Trace,
            LogLevel.Debug => SentryLogLevel.Debug,
            LogLevel.Information => SentryLogLevel.Info,
            LogLevel.Warning => SentryLogLevel.Warning,
            LogLevel.Error => SentryLogLevel.Error,
            LogLevel.Critical => SentryLogLevel.Fatal,
            LogLevel.None => SentryLogLevel.Trace,
            _ => SentryLogLevel.Trace,
        };
    }
}
