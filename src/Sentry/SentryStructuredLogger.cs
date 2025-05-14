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
public sealed class SentryStructuredLogger
{
    private readonly IHub _hub;
    private readonly ISystemClock _clock;

    private readonly SentryOptions? _options;
    private readonly IInternalScopeManager? _scopeManager;

    internal SentryStructuredLogger(IHub hub)
        : this(hub, (hub as Hub)?.ScopeManager, hub.GetSentryOptions(), SystemClock.Clock)
    {
    }

    internal SentryStructuredLogger(IHub hub, IInternalScopeManager? scopeManager, SentryOptions? options, ISystemClock clock)
    {
        _hub = hub;
        _clock = clock;

        _options = options;
        _scopeManager = scopeManager;
        IsEnabled = options is { EnableLogs: true };
    }

    [MemberNotNullWhen(true, nameof(_options))]
    private bool IsEnabled { get; }

    /// <summary>
    /// Creates and sends a structured log to Sentry, with severity <see cref="SentryLogLevel.Trace"/>, when enabled and sampled.
    /// <para>This API is experimental and it may change in the future.</para>
    /// </summary>
    public void LogTrace(string template, object[]? parameters = null, Action<SentryLog>? configureLog = null)
    {
        if (IsEnabled)
        {
            CaptureLog(SentryLogLevel.Trace, template, parameters, configureLog);
        }
    }

    /// <summary>
    /// Creates and sends a structured log to Sentry, with severity <see cref="SentryLogLevel.Debug"/>, when enabled and sampled.
    /// <para>This API is experimental and it may change in the future.</para>
    /// </summary>
    [Experimental(DiagnosticId.ExperimentalFeature)]
    public void LogDebug(string template, object[]? parameters = null, Action<SentryLog>? configureLog = null)
    {
        if (IsEnabled)
        {
            CaptureLog(SentryLogLevel.Debug, template, parameters, configureLog);
        }
    }

    /// <summary>
    /// Creates and sends a structured log to Sentry, with severity <see cref="SentryLogLevel.Info"/>, when enabled and sampled.
    /// <para>This API is experimental and it may change in the future.</para>
    /// </summary>
    [Experimental(DiagnosticId.ExperimentalFeature)]
    public void LogInfo(string template, object[]? parameters = null, Action<SentryLog>? configureLog = null)
    {
        if (IsEnabled)
        {
            CaptureLog(SentryLogLevel.Info, template, parameters, configureLog);
        }
    }

    /// <summary>
    /// Creates and sends a structured log to Sentry, with severity <see cref="SentryLogLevel.Warning"/>, when enabled and sampled.
    /// <para>This API is experimental and it may change in the future.</para>
    /// </summary>
    [Experimental(DiagnosticId.ExperimentalFeature)]
    public void LogWarning(string template, object[]? parameters = null, Action<SentryLog>? configureLog = null)
    {
        if (IsEnabled)
        {
            CaptureLog(SentryLogLevel.Warning, template, parameters, configureLog);
        }
    }

    /// <summary>
    /// Creates and sends a structured log to Sentry, with severity <see cref="SentryLogLevel.Error"/>, when enabled and sampled.
    /// <para>This API is experimental and it may change in the future.</para>
    /// </summary>
    [Experimental(DiagnosticId.ExperimentalFeature)]
    public void LogError(string template, object[]? parameters = null, Action<SentryLog>? configureLog = null)
    {
        if (IsEnabled)
        {
            CaptureLog(SentryLogLevel.Error, template, parameters, configureLog);
        }
    }

    /// <summary>
    /// Creates and sends a structured log to Sentry, with severity <see cref="SentryLogLevel.Fatal"/>, when enabled and sampled.
    /// <para>This API is experimental and it may change in the future.</para>
    /// </summary>
    [Experimental(DiagnosticId.ExperimentalFeature)]
    public void LogFatal(string template, object[]? parameters = null, Action<SentryLog>? configureLog = null)
    {
        if (IsEnabled)
        {
            CaptureLog(SentryLogLevel.Fatal, template, parameters, configureLog);
        }
    }

    private void CaptureLog(SentryLogLevel level, string template, object[]? parameters, Action<SentryLog>? configureLog)
    {
        Debug.Assert(_options is not null);

        var timestamp = _clock.GetUtcNow();

        if (!TryGetTraceId(_hub, _scopeManager, out var traceId))
        {
            _options.DiagnosticLogger?.LogWarning("TraceId not found");
        }

        _ = TryGetParentSpanId(_hub, _scopeManager, out var parentSpanId);

        var message = string.Format(CultureInfo.InvariantCulture, template, parameters ?? []);
        SentryLog log = new(timestamp, traceId, level, message)
        {
            Template = template,
            Parameters = ImmutableArray.Create(parameters),
            ParentSpanId = parentSpanId,
        };

        try
        {
            configureLog?.Invoke(log);
        }
        catch (Exception e)
        {
            //TODO: change to Diagnostic Logger (if enabled)
            // see https://github.com/getsentry/sentry-dotnet/issues/4132
            Console.WriteLine(e);
            return;
        }

        log.SetAttributes(_options);

        var configuredLog = log;
        if (_options.BeforeSendLogInternal is { } beforeSendLog)
        {
            try
            {
                configuredLog = beforeSendLog.Invoke(log);
            }
            catch (Exception e)
            {
                //TODO: change to Diagnostic Logger (if enabled)
                // see https://github.com/getsentry/sentry-dotnet/issues/4132
                Console.WriteLine(e);
                return;
            }
        }

        if (configuredLog is not null)
        {
            //TODO: enqueue in Batch-Processor / Background-Worker
            // see https://github.com/getsentry/sentry-dotnet/issues/4132
            _ = _hub.CaptureEnvelope(Envelope.FromLog(configuredLog));
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
