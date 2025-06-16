namespace Sentry.Internal;

internal sealed class NoOpSentryStructuredLogger : SentryStructuredLogger
{
    internal static NoOpSentryStructuredLogger Instance { get; } = new NoOpSentryStructuredLogger();

    private NoOpSentryStructuredLogger()
    {
    }

    private protected override void CaptureLog(SentryLogLevel level, string template, object[]? parameters, Action<SentryLog>? configureLog)
    {
        // disabled
    }
}
