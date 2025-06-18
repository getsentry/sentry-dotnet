namespace Sentry.Internal;

internal sealed class DisabledSentryStructuredLogger : SentryStructuredLogger
{
    internal static DisabledSentryStructuredLogger Instance { get; } = new DisabledSentryStructuredLogger();

    internal DisabledSentryStructuredLogger()
    {
    }

    private protected override void CaptureLog(SentryLogLevel level, string template, object[]? parameters, Action<SentryLog>? configureLog)
    {
        // disabled
    }
}
