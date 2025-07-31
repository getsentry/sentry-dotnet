using Sentry.Infrastructure;
using Sentry.Internal;

namespace Sentry;

/// <summary>
/// Creates and sends logs to Sentry.
/// <para>This API is experimental and it may change in the future.</para>
/// </summary>
[Experimental(DiagnosticId.ExperimentalFeature)]
public abstract class SentryStructuredLogger : IDisposable
{
    internal static SentryStructuredLogger Create(IHub hub, SentryOptions options, ISystemClock clock)
        => Create(hub, options, clock, 100, TimeSpan.FromSeconds(5));

    internal static SentryStructuredLogger Create(IHub hub, SentryOptions options, ISystemClock clock, int batchCount, TimeSpan batchInterval)
    {
        return options.Experimental.EnableLogs
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

    /// <summary>
    /// Creates and sends a structured log to Sentry, with severity <see cref="SentryLogLevel.Trace"/>, when enabled and sampled.
    /// <para>This API is experimental and it may change in the future.</para>
    /// </summary>
    /// <param name="template">A formattable <see langword="string"/>. When incompatible with the <paramref name="parameters"/>, the log will not be captured. See <see href="https://learn.microsoft.com/dotnet/api/system.string.format">System.String.Format</see>.</param>
    /// <param name="parameters">The arguments to the <paramref name="template"/>. See <see href="https://learn.microsoft.com/dotnet/api/system.string.format">System.String.Format</see>.</param>
    /// <param name="configureLog">A delegate to set attributes on the <see cref="SentryLog"/>. When the delegate throws an <see cref="Exception"/> during invocation, the log will not be captured.</param>
    [Experimental(DiagnosticId.ExperimentalFeature)]
    public void LogTrace(string template, object[]? parameters = null, Action<SentryLog>? configureLog = null)
    {
        CaptureLog(SentryLogLevel.Trace, template, parameters, configureLog);
    }

    /// <summary>
    /// Creates and sends a structured log to Sentry, with severity <see cref="SentryLogLevel.Debug"/>, when enabled and sampled.
    /// <para>This API is experimental and it may change in the future.</para>
    /// </summary>
    /// <param name="template">A formattable <see langword="string"/>. When incompatible with the <paramref name="parameters"/>, the log will not be captured. See <see href="https://learn.microsoft.com/dotnet/api/system.string.format">System.String.Format</see>.</param>
    /// <param name="parameters">The arguments to the <paramref name="template"/>. See <see href="https://learn.microsoft.com/dotnet/api/system.string.format">System.String.Format</see>.</param>
    /// <param name="configureLog">A delegate to set attributes on the <see cref="SentryLog"/>. When the delegate throws an <see cref="Exception"/> during invocation, the log will not be captured.</param>
    [Experimental(DiagnosticId.ExperimentalFeature)]
    public void LogDebug(string template, object[]? parameters = null, Action<SentryLog>? configureLog = null)
    {
        CaptureLog(SentryLogLevel.Debug, template, parameters, configureLog);
    }

    /// <summary>
    /// Creates and sends a structured log to Sentry, with severity <see cref="SentryLogLevel.Info"/>, when enabled and sampled.
    /// <para>This API is experimental and it may change in the future.</para>
    /// </summary>
    /// <param name="template">A formattable <see langword="string"/>. When incompatible with the <paramref name="parameters"/>, the log will not be captured. See <see href="https://learn.microsoft.com/dotnet/api/system.string.format">System.String.Format</see>.</param>
    /// <param name="parameters">The arguments to the <paramref name="template"/>. See <see href="https://learn.microsoft.com/dotnet/api/system.string.format">System.String.Format</see>.</param>
    /// <param name="configureLog">A delegate to set attributes on the <see cref="SentryLog"/>. When the delegate throws an <see cref="Exception"/> during invocation, the log will not be captured.</param>
    [Experimental(DiagnosticId.ExperimentalFeature)]
    public void LogInfo(string template, object[]? parameters = null, Action<SentryLog>? configureLog = null)
    {
        CaptureLog(SentryLogLevel.Info, template, parameters, configureLog);
    }

    /// <summary>
    /// Creates and sends a structured log to Sentry, with severity <see cref="SentryLogLevel.Warning"/>, when enabled and sampled.
    /// <para>This API is experimental and it may change in the future.</para>
    /// </summary>
    /// <param name="template">A formattable <see langword="string"/>. When incompatible with the <paramref name="parameters"/>, the log will not be captured. See <see href="https://learn.microsoft.com/dotnet/api/system.string.format">System.String.Format</see>.</param>
    /// <param name="parameters">The arguments to the <paramref name="template"/>. See <see href="https://learn.microsoft.com/dotnet/api/system.string.format">System.String.Format</see>.</param>
    /// <param name="configureLog">A delegate to set attributes on the <see cref="SentryLog"/>. When the delegate throws an <see cref="Exception"/> during invocation, the log will not be captured.</param>
    [Experimental(DiagnosticId.ExperimentalFeature)]
    public void LogWarning(string template, object[]? parameters = null, Action<SentryLog>? configureLog = null)
    {
        CaptureLog(SentryLogLevel.Warning, template, parameters, configureLog);
    }

    /// <summary>
    /// Creates and sends a structured log to Sentry, with severity <see cref="SentryLogLevel.Error"/>, when enabled and sampled.
    /// <para>This API is experimental and it may change in the future.</para>
    /// </summary>
    /// <param name="template">A formattable <see langword="string"/>. When incompatible with the <paramref name="parameters"/>, the log will not be captured. See <see href="https://learn.microsoft.com/dotnet/api/system.string.format">System.String.Format</see>.</param>
    /// <param name="parameters">The arguments to the <paramref name="template"/>. See <see href="https://learn.microsoft.com/dotnet/api/system.string.format">System.String.Format</see>.</param>
    /// <param name="configureLog">A delegate to set attributes on the <see cref="SentryLog"/>. When the delegate throws an <see cref="Exception"/> during invocation, the log will not be captured.</param>
    [Experimental(DiagnosticId.ExperimentalFeature)]
    public void LogError(string template, object[]? parameters = null, Action<SentryLog>? configureLog = null)
    {
        CaptureLog(SentryLogLevel.Error, template, parameters, configureLog);
    }

    /// <summary>
    /// Creates and sends a structured log to Sentry, with severity <see cref="SentryLogLevel.Fatal"/>, when enabled and sampled.
    /// <para>This API is experimental and it may change in the future.</para>
    /// </summary>
    /// <param name="template">A formattable <see langword="string"/>. When incompatible with the <paramref name="parameters"/>, the log will not be captured. See <see href="https://learn.microsoft.com/dotnet/api/system.string.format">System.String.Format</see>.</param>
    /// <param name="parameters">The arguments to the <paramref name="template"/>. See <see href="https://learn.microsoft.com/dotnet/api/system.string.format">System.String.Format</see>.</param>
    /// <param name="configureLog">A delegate to set attributes on the <see cref="SentryLog"/>. When the delegate throws an <see cref="Exception"/> during invocation, the log will not be captured.</param>
    [Experimental(DiagnosticId.ExperimentalFeature)]
    public void LogFatal(string template, object[]? parameters = null, Action<SentryLog>? configureLog = null)
    {
        CaptureLog(SentryLogLevel.Fatal, template, parameters, configureLog);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Override in inherited types to clean up managed and unmanaged resources.
    /// </summary>
    /// <param name="disposing">Invoked from <see cref="Dispose()"/> when <see langword="true"/>; Invoked from <c>Finalize</c> when <see langword="false"/>.</param>
    protected virtual void Dispose(bool disposing)
    {
    }
}
