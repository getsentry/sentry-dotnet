namespace Sentry.Serilog;

internal static class SentryExtensions
{
    public static bool TryGetSourceContext(this LogEvent logEvent, [NotNullWhen(true)] out string? sourceContext)
    {
        if (logEvent.Properties.TryGetValue("SourceContext", out var prop) &&
            prop is ScalarValue { Value: string sourceContextValue })
        {
            sourceContext = sourceContextValue;
            return true;
        }

        sourceContext = null;
        return false;
    }

    public static SentryLevel? ToSentryLevel(this LogEventLevel loggingLevel)
    {
        return loggingLevel switch
        {
            LogEventLevel.Fatal => SentryLevel.Fatal,
            LogEventLevel.Error => SentryLevel.Error,
            LogEventLevel.Warning => SentryLevel.Warning,
            LogEventLevel.Information => SentryLevel.Info,
            LogEventLevel.Debug => SentryLevel.Debug,
            LogEventLevel.Verbose => SentryLevel.Debug,
            _ => null
        };
    }

    public static BreadcrumbLevel ToBreadcrumbLevel(this LogEventLevel level)
    {
        return level switch
        {
            LogEventLevel.Verbose => BreadcrumbLevel.Debug,
            LogEventLevel.Debug => BreadcrumbLevel.Debug,
            LogEventLevel.Information => BreadcrumbLevel.Info,
            LogEventLevel.Warning => BreadcrumbLevel.Warning,
            LogEventLevel.Error => BreadcrumbLevel.Error,
            LogEventLevel.Fatal => BreadcrumbLevel.Critical,
            _ => (BreadcrumbLevel)level
        };
    }
}
