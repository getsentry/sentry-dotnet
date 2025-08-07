namespace Sentry.Internal;

internal sealed class DisabledSentryStructuredLogger : SentryStructuredLogger
{
    internal static DisabledSentryStructuredLogger Instance { get; } = new DisabledSentryStructuredLogger();

    internal DisabledSentryStructuredLogger()
    {
    }

    /// <inheritdoc />
    private protected override void CaptureLog(SentryLogLevel level, string template, object[]? parameters, Action<SentryLog>? configureLog)
    {
        // disabled
    }

    /// <inheritdoc />
    protected internal override void CaptureLog(SentryLog log)
    {
        // disabled
    }

    /// <inheritdoc />
    protected internal override void Flush()
    {
        // disabled
    }
}
