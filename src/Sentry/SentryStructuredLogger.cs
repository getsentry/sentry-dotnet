using Sentry.Infrastructure;
using Sentry.Internal;

namespace Sentry;

/// <summary>
/// Creates and sends logs to Sentry.
/// </summary>
public abstract partial class SentryStructuredLogger
{
    internal static SentryStructuredLogger Create(IHub hub, SentryOptions options, ISystemClock clock)
        => Create(hub, options, clock, 100, TimeSpan.FromSeconds(5));

    internal static SentryStructuredLogger Create(IHub hub, SentryOptions options, ISystemClock clock, int batchCount, TimeSpan batchInterval)
    {
        return options.EnableLogs
            ? new DefaultSentryStructuredLogger(hub, options, clock, batchCount, batchInterval)
            : DisabledSentryStructuredLogger.Instance;
    }

    private protected SentryStructuredLogger()
    {
    }

    /// <summary>
    /// Buffers a <see href="https://develop.sentry.dev/sdk/telemetry/logs">Sentry Log</see> message
    /// via the associated <see href="https://develop.sentry.dev/sdk/telemetry/spans/batch-processor">Batch Processor</see>.
    /// </summary>
    /// <param name="level">The severity level of the log.</param>
    /// <param name="template">The parameterized template string.</param>
    /// <param name="parameters">The parameters to the <paramref name="template"/> string.</param>
    /// <param name="configureLog">A configuration callback. Will be removed in a future version.</param>
    private protected abstract void CaptureLog(SentryLogLevel level, string template, object[]? parameters, Action<SentryLog>? configureLog);

    /// <summary>
    /// Buffers a <see href="https://develop.sentry.dev/sdk/telemetry/logs">Sentry Log</see> message
    /// via the associated <see href="https://develop.sentry.dev/sdk/telemetry/spans/batch-processor">Batch Processor</see>.
    /// </summary>
    /// <param name="log">The log.</param>
    protected internal abstract void CaptureLog(SentryLog log);

    /// <summary>
    /// Clears all buffers for this logger and causes any buffered logs to be sent by the underlying <see cref="ISentryClient"/>.
    /// </summary>
    protected internal abstract void Flush();
}
