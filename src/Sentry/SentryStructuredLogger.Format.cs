using Sentry.Infrastructure;

namespace Sentry;

public abstract partial class SentryStructuredLogger
{
    /// <summary>
    /// Creates and sends a structured log to Sentry, with severity <see cref="SentryLogLevel.Trace"/>, when enabled and sampled.
    /// <para>This API is experimental and it may change in the future.</para>
    /// </summary>
    /// <param name="template">A formattable <see langword="string"/>. When incompatible with the <paramref name="parameters"/>, the log will not be captured. See <see href="https://learn.microsoft.com/dotnet/api/system.string.format">System.String.Format</see>.</param>
    /// <param name="parameters">The arguments to the <paramref name="template"/>. See <see href="https://learn.microsoft.com/dotnet/api/system.string.format">System.String.Format</see>.</param>
    [Experimental(DiagnosticId.ExperimentalFeature)]
    public void LogTrace(string template, params object[] parameters)
    {
        CaptureLog(SentryLogLevel.Trace, template, parameters, null);
    }

    /// <summary>
    /// Creates and sends a structured log to Sentry, with severity <see cref="SentryLogLevel.Trace"/>, when enabled and sampled.
    /// <para>This API is experimental and it may change in the future.</para>
    /// </summary>
    /// <param name="configureLog">A delegate to set attributes on the <see cref="SentryLog"/>. When the delegate throws an <see cref="Exception"/> during invocation, the log will not be captured.</param>
    /// <param name="template">A formattable <see langword="string"/>. When incompatible with the <paramref name="parameters"/>, the log will not be captured. See <see href="https://learn.microsoft.com/dotnet/api/system.string.format">System.String.Format</see>.</param>
    /// <param name="parameters">The arguments to the <paramref name="template"/>. See <see href="https://learn.microsoft.com/dotnet/api/system.string.format">System.String.Format</see>.</param>
    [Experimental(DiagnosticId.ExperimentalFeature)]
    public void LogTrace(Action<SentryLog> configureLog, string template, params object[] parameters)
    {
        CaptureLog(SentryLogLevel.Trace, template, parameters, configureLog);
    }

    /// <summary>
    /// Creates and sends a structured log to Sentry, with severity <see cref="SentryLogLevel.Debug"/>, when enabled and sampled.
    /// <para>This API is experimental and it may change in the future.</para>
    /// </summary>
    /// <param name="template">A formattable <see langword="string"/>. When incompatible with the <paramref name="parameters"/>, the log will not be captured. See <see href="https://learn.microsoft.com/dotnet/api/system.string.format">System.String.Format</see>.</param>
    /// <param name="parameters">The arguments to the <paramref name="template"/>. See <see href="https://learn.microsoft.com/dotnet/api/system.string.format">System.String.Format</see>.</param>
    [Experimental(DiagnosticId.ExperimentalFeature)]
    public void LogDebug(string template, params object[] parameters)
    {
        CaptureLog(SentryLogLevel.Debug, template, parameters, null);
    }

    /// <summary>
    /// Creates and sends a structured log to Sentry, with severity <see cref="SentryLogLevel.Debug"/>, when enabled and sampled.
    /// <para>This API is experimental and it may change in the future.</para>
    /// </summary>
    /// <param name="configureLog">A delegate to set attributes on the <see cref="SentryLog"/>. When the delegate throws an <see cref="Exception"/> during invocation, the log will not be captured.</param>
    /// <param name="template">A formattable <see langword="string"/>. When incompatible with the <paramref name="parameters"/>, the log will not be captured. See <see href="https://learn.microsoft.com/dotnet/api/system.string.format">System.String.Format</see>.</param>
    /// <param name="parameters">The arguments to the <paramref name="template"/>. See <see href="https://learn.microsoft.com/dotnet/api/system.string.format">System.String.Format</see>.</param>
    [Experimental(DiagnosticId.ExperimentalFeature)]
    public void LogDebug(Action<SentryLog> configureLog, string template, params object[] parameters)
    {
        CaptureLog(SentryLogLevel.Debug, template, parameters, configureLog);
    }

    /// <summary>
    /// Creates and sends a structured log to Sentry, with severity <see cref="SentryLogLevel.Info"/>, when enabled and sampled.
    /// <para>This API is experimental and it may change in the future.</para>
    /// </summary>
    /// <param name="template">A formattable <see langword="string"/>. When incompatible with the <paramref name="parameters"/>, the log will not be captured. See <see href="https://learn.microsoft.com/dotnet/api/system.string.format">System.String.Format</see>.</param>
    /// <param name="parameters">The arguments to the <paramref name="template"/>. See <see href="https://learn.microsoft.com/dotnet/api/system.string.format">System.String.Format</see>.</param>
    [Experimental(DiagnosticId.ExperimentalFeature)]
    public void LogInfo(string template, params object[] parameters)
    {
        CaptureLog(SentryLogLevel.Info, template, parameters, null);
    }

    /// <summary>
    /// Creates and sends a structured log to Sentry, with severity <see cref="SentryLogLevel.Info"/>, when enabled and sampled.
    /// <para>This API is experimental and it may change in the future.</para>
    /// </summary>
    /// <param name="configureLog">A delegate to set attributes on the <see cref="SentryLog"/>. When the delegate throws an <see cref="Exception"/> during invocation, the log will not be captured.</param>
    /// <param name="template">A formattable <see langword="string"/>. When incompatible with the <paramref name="parameters"/>, the log will not be captured. See <see href="https://learn.microsoft.com/dotnet/api/system.string.format">System.String.Format</see>.</param>
    /// <param name="parameters">The arguments to the <paramref name="template"/>. See <see href="https://learn.microsoft.com/dotnet/api/system.string.format">System.String.Format</see>.</param>
    [Experimental(DiagnosticId.ExperimentalFeature)]
    public void LogInfo(Action<SentryLog> configureLog, string template, params object[] parameters)
    {
        CaptureLog(SentryLogLevel.Info, template, parameters, configureLog);
    }

    /// <summary>
    /// Creates and sends a structured log to Sentry, with severity <see cref="SentryLogLevel.Warning"/>, when enabled and sampled.
    /// <para>This API is experimental and it may change in the future.</para>
    /// </summary>
    /// <param name="template">A formattable <see langword="string"/>. When incompatible with the <paramref name="parameters"/>, the log will not be captured. See <see href="https://learn.microsoft.com/dotnet/api/system.string.format">System.String.Format</see>.</param>
    /// <param name="parameters">The arguments to the <paramref name="template"/>. See <see href="https://learn.microsoft.com/dotnet/api/system.string.format">System.String.Format</see>.</param>
    [Experimental(DiagnosticId.ExperimentalFeature)]
    public void LogWarning(string template, params object[] parameters)
    {
        CaptureLog(SentryLogLevel.Warning, template, parameters, null);
    }

    /// <summary>
    /// Creates and sends a structured log to Sentry, with severity <see cref="SentryLogLevel.Warning"/>, when enabled and sampled.
    /// <para>This API is experimental and it may change in the future.</para>
    /// </summary>
    /// <param name="configureLog">A delegate to set attributes on the <see cref="SentryLog"/>. When the delegate throws an <see cref="Exception"/> during invocation, the log will not be captured.</param>
    /// <param name="template">A formattable <see langword="string"/>. When incompatible with the <paramref name="parameters"/>, the log will not be captured. See <see href="https://learn.microsoft.com/dotnet/api/system.string.format">System.String.Format</see>.</param>
    /// <param name="parameters">The arguments to the <paramref name="template"/>. See <see href="https://learn.microsoft.com/dotnet/api/system.string.format">System.String.Format</see>.</param>
    [Experimental(DiagnosticId.ExperimentalFeature)]
    public void LogWarning(Action<SentryLog> configureLog, string template, params object[] parameters)
    {
        CaptureLog(SentryLogLevel.Warning, template, parameters, configureLog);
    }

    /// <summary>
    /// Creates and sends a structured log to Sentry, with severity <see cref="SentryLogLevel.Error"/>, when enabled and sampled.
    /// <para>This API is experimental and it may change in the future.</para>
    /// </summary>
    /// <param name="template">A formattable <see langword="string"/>. When incompatible with the <paramref name="parameters"/>, the log will not be captured. See <see href="https://learn.microsoft.com/dotnet/api/system.string.format">System.String.Format</see>.</param>
    /// <param name="parameters">The arguments to the <paramref name="template"/>. See <see href="https://learn.microsoft.com/dotnet/api/system.string.format">System.String.Format</see>.</param>
    [Experimental(DiagnosticId.ExperimentalFeature)]
    public void LogError(string template, params object[] parameters)
    {
        CaptureLog(SentryLogLevel.Error, template, parameters, null);
    }

    /// <summary>
    /// Creates and sends a structured log to Sentry, with severity <see cref="SentryLogLevel.Error"/>, when enabled and sampled.
    /// <para>This API is experimental and it may change in the future.</para>
    /// </summary>
    /// <param name="configureLog">A delegate to set attributes on the <see cref="SentryLog"/>. When the delegate throws an <see cref="Exception"/> during invocation, the log will not be captured.</param>
    /// <param name="template">A formattable <see langword="string"/>. When incompatible with the <paramref name="parameters"/>, the log will not be captured. See <see href="https://learn.microsoft.com/dotnet/api/system.string.format">System.String.Format</see>.</param>
    /// <param name="parameters">The arguments to the <paramref name="template"/>. See <see href="https://learn.microsoft.com/dotnet/api/system.string.format">System.String.Format</see>.</param>
    [Experimental(DiagnosticId.ExperimentalFeature)]
    public void LogError(Action<SentryLog> configureLog, string template, params object[] parameters)
    {
        CaptureLog(SentryLogLevel.Error, template, parameters, configureLog);
    }

    /// <summary>
    /// Creates and sends a structured log to Sentry, with severity <see cref="SentryLogLevel.Fatal"/>, when enabled and sampled.
    /// <para>This API is experimental and it may change in the future.</para>
    /// </summary>
    /// <param name="template">A formattable <see langword="string"/>. When incompatible with the <paramref name="parameters"/>, the log will not be captured. See <see href="https://learn.microsoft.com/dotnet/api/system.string.format">System.String.Format</see>.</param>
    /// <param name="parameters">The arguments to the <paramref name="template"/>. See <see href="https://learn.microsoft.com/dotnet/api/system.string.format">System.String.Format</see>.</param>
    [Experimental(DiagnosticId.ExperimentalFeature)]
    public void LogFatal(string template, params object[] parameters)
    {
        CaptureLog(SentryLogLevel.Fatal, template, parameters, null);
    }

    /// <summary>
    /// Creates and sends a structured log to Sentry, with severity <see cref="SentryLogLevel.Fatal"/>, when enabled and sampled.
    /// <para>This API is experimental and it may change in the future.</para>
    /// </summary>
    /// <param name="configureLog">A delegate to set attributes on the <see cref="SentryLog"/>. When the delegate throws an <see cref="Exception"/> during invocation, the log will not be captured.</param>
    /// <param name="template">A formattable <see langword="string"/>. When incompatible with the <paramref name="parameters"/>, the log will not be captured. See <see href="https://learn.microsoft.com/dotnet/api/system.string.format">System.String.Format</see>.</param>
    /// <param name="parameters">The arguments to the <paramref name="template"/>. See <see href="https://learn.microsoft.com/dotnet/api/system.string.format">System.String.Format</see>.</param>
    [Experimental(DiagnosticId.ExperimentalFeature)]
    public void LogFatal(Action<SentryLog> configureLog, string template, params object[] parameters)
    {
        CaptureLog(SentryLogLevel.Fatal, template, parameters, configureLog);
    }
}
