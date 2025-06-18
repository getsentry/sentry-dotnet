using Sentry.Infrastructure;
using Sentry.Internal;

namespace Sentry;

/// <summary>
/// Creates and sends logs to Sentry.
/// <para>This API is experimental and it may change in the future.</para>
/// </summary>
[Experimental(DiagnosticId.ExperimentalFeature)]
public abstract class SentryStructuredLogger
{
    internal static SentryStructuredLogger Create(IHub hub, IInternalScopeManager scopeManager, SentryOptions options, ISystemClock clock)
    {
        return options.Experimental.EnableLogs
            ? new DefaultSentryStructuredLogger(hub, scopeManager, options, clock)
            : DisabledSentryStructuredLogger.Instance;
    }

    private protected SentryStructuredLogger()
    {
    }

    private protected abstract void CaptureLog(SentryLogLevel level, string template, object[]? parameters, Action<SentryLog>? configureLog);

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
}
