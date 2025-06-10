using Sentry.Infrastructure;

namespace Sentry;

/// <summary>
/// Creates and sends logs to Sentry.
/// <para>This API is experimental and it may change in the future.</para>
/// </summary>
[Experimental(DiagnosticId.ExperimentalFeature)]
public abstract partial class SentryStructuredLogger
{
    private protected SentryStructuredLogger()
    {
    }

    private protected abstract void CaptureLog(SentryLogLevel level, string template, object[]? parameters, Action<SentryLog>? configureLog);
}
