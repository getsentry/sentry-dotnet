using Sentry.Extensibility;
using Sentry.Infrastructure;
using Sentry.Internal;
using Sentry.Protocol;
using Sentry.Protocol.Envelopes;

namespace Sentry;

/// <summary>
/// Creates and sends logs to Sentry.
/// <para>This API is experimental and it may change in the future.</para>
/// </summary>
[Experimental(DiagnosticId.ExperimentalFeature)]
public sealed class SentryLogger
{
    private readonly RandomValuesFactory _randomValuesFactory;

    internal SentryLogger()
    {
        _randomValuesFactory = new SynchronizedRandomValuesFactory();
    }

    /// <summary>
    /// Creates and sends a structured log to Sentry, with severity <see cref="LogSeverityLevel.Trace"/>, when enabled and sampled.
    /// <para>This API is experimental and it may change in the future.</para>
    /// </summary>
    public void Trace(string template, object[]? parameters = null, Action<SentryLog>? configureLog = null)
    {
        if (IsEnabled())
        {
            CaptureLog(LogSeverityLevel.Trace, template, parameters, configureLog);
        }
    }

    /// <summary>
    /// Creates and sends a structured log to Sentry, with severity <see cref="LogSeverityLevel.Debug"/>, when enabled and sampled.
    /// <para>This API is experimental and it may change in the future.</para>
    /// </summary>
    [Experimental(DiagnosticId.ExperimentalFeature)]
    public void Debug(string template, object[]? parameters = null, Action<SentryLog>? configureLog = null)
    {
        if (IsEnabled())
        {
            CaptureLog(LogSeverityLevel.Debug, template, parameters, configureLog);
        }
    }

    /// <summary>
    /// Creates and sends a structured log to Sentry, with severity <see cref="LogSeverityLevel.Info"/>, when enabled and sampled.
    /// <para>This API is experimental and it may change in the future.</para>
    /// </summary>
    [Experimental(DiagnosticId.ExperimentalFeature)]
    public void Info(string template, object[]? parameters = null, Action<SentryLog>? configureLog = null)
    {
        if (IsEnabled())
        {
            CaptureLog(LogSeverityLevel.Info, template, parameters, configureLog);
        }
    }

    /// <summary>
    /// Creates and sends a structured log to Sentry, with severity <see cref="LogSeverityLevel.Warn"/>, when enabled and sampled.
    /// <para>This API is experimental and it may change in the future.</para>
    /// </summary>
    [Experimental(DiagnosticId.ExperimentalFeature)]
    public void Warn(string template, object[]? parameters = null, Action<SentryLog>? configureLog = null)
    {
        if (IsEnabled())
        {
            CaptureLog(LogSeverityLevel.Warn, template, parameters, configureLog);
        }
    }

    /// <summary>
    /// Creates and sends a structured log to Sentry, with severity <see cref="LogSeverityLevel.Error"/>, when enabled and sampled.
    /// <para>This API is experimental and it may change in the future.</para>
    /// </summary>
    [Experimental(DiagnosticId.ExperimentalFeature)]
    public void Error(string template, object[]? parameters = null, Action<SentryLog>? configureLog = null)
    {
        if (IsEnabled())
        {
            CaptureLog(LogSeverityLevel.Error, template, parameters, configureLog);
        }
    }

    /// <summary>
    /// Creates and sends a structured log to Sentry, with severity <see cref="LogSeverityLevel.Fatal"/>, when enabled and sampled.
    /// <para>This API is experimental and it may change in the future.</para>
    /// </summary>
    [Experimental(DiagnosticId.ExperimentalFeature)]
    public void Fatal(string template, object[]? parameters = null, Action<SentryLog>? configureLog = null)
    {
        if (IsEnabled())
        {
            CaptureLog(LogSeverityLevel.Fatal, template, parameters, configureLog);
        }
    }

    private bool IsEnabled()
    {
        var hub = SentrySdk.CurrentHub;

        if (hub.GetSentryOptions() is { } options)
        {
            return options.EnableLogs;
        }

        return false;
    }

    private void CaptureLog(LogSeverityLevel level, string template, object[]? parameters, Action<SentryLog>? configureLog)
    {
        var timestamp = DateTimeOffset.UtcNow;

        var hub = SentrySdk.CurrentHub;

        if (hub.GetSentryOptions() is not { EnableLogs: true } options)
        {
            return;
        }

        if (!_randomValuesFactory.NextBool(options.LogsSampleRate))
        {
            return;
        }

        var scopeManager = (hub as Hub)?.ScopeManager;

        if (!TryGetTraceId(hub, scopeManager, out var traceId))
        {
            options.DiagnosticLogger?.LogWarning("TraceId not found");
        }

        _ = TryGetParentSpanId(hub, scopeManager, out var parentSpanId);

        var message = string.Format(template, parameters ?? []);
        SentryLog log = new(timestamp, traceId, level, message)
        {
            Template = template,
            Parameters = ImmutableArray.Create(parameters),
        };
        log.SetAttributes(options, parentSpanId);

        SentryLog? configuredLog;
        try
        {
            configureLog?.Invoke(log);
            configuredLog = options.BeforeSendLogInternal?.Invoke(log);
        }
        catch (Exception e)
        {
            //TODO: change to Diagnostic Logger (if enabled)
            // see https://github.com/getsentry/sentry-dotnet/issues/4132
            Console.WriteLine(e);
            return;
        }

        if (configuredLog is not null)
        {
            //TODO: enqueue in Batch-Processor / Background-Worker
            // see https://github.com/getsentry/sentry-dotnet/issues/4132
            _ = hub.CaptureEnvelope(Envelope.FromLog(configuredLog));
        }
    }

    private static bool TryGetTraceId(IHub hub, IInternalScopeManager? scopeManager, out SentryId traceId)
    {
        if (hub.GetSpan() is { } span)
        {
            traceId = span.TraceId;
            return true;
        }

        if (scopeManager is not null)
        {
            var currentScope = scopeManager.GetCurrent().Key;
            traceId = currentScope.PropagationContext.TraceId;
            return true;
        }

        traceId = SentryId.Empty;
        return false;
    }

    private static bool TryGetParentSpanId(IHub hub, IInternalScopeManager? scopeManager, out SpanId? parentSpanId)
    {
        if (hub.GetSpan() is { } span && span.ParentSpanId.HasValue)
        {
            parentSpanId = span.ParentSpanId;
            return true;
        }

        if (scopeManager is not null)
        {
            var currentScope = scopeManager.GetCurrent().Key;
            parentSpanId = currentScope.PropagationContext.ParentSpanId;
            return true;
        }

        parentSpanId = null;
        return false;
    }
}
