using Sentry.Extensibility;
using Sentry.Infrastructure;
using Sentry.Protocol.Envelopes;

namespace Sentry.Internal;

internal sealed class DefaultSentryStructuredLogger : SentryStructuredLogger
{
    private readonly IHub _hub;
    private readonly IInternalScopeManager _scopeManager;
    private readonly SentryOptions _options;
    private readonly ISystemClock _clock;

    private readonly bool _isEnabled;

    internal DefaultSentryStructuredLogger(IHub hub, IInternalScopeManager scopeManager, SentryOptions options, ISystemClock clock)
    {
        _hub = hub;
        _scopeManager = scopeManager;
        _options = options;
        _clock = clock;

        _isEnabled = options is { Experimental.EnableLogs: true };
    }

    private protected override void CaptureLog(SentryLogLevel level, string template, object[]? parameters, Action<SentryLog>? configureLog)
    {
        if (!_isEnabled)
        {
            return;
        }

        var timestamp = _clock.GetUtcNow();

        if (!TryGetTraceId(_hub, _scopeManager, out var traceId))
        {
            _options.DiagnosticLogger?.LogWarning("TraceId not found");
        }

        _ = TryGetParentSpanId(_hub, _scopeManager, out var parentSpanId);

        string message;
        try
        {
            message = string.Format(CultureInfo.InvariantCulture, template, parameters ?? []);
        }
        catch (FormatException e)
        {
            _options.DiagnosticLogger?.LogError(e, "Template string does not match the provided argument. The Log will be dropped.");
            return;
        }

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
            _options.DiagnosticLogger?.LogError(e, "The configureLog callback threw an exception. The Log will be dropped.");
            return;
        }

        log.SetAttributes(_options);

        var configuredLog = log;
        if (_options.Experimental.BeforeSendLogInternal is { } beforeSendLog)
        {
            try
            {
                configuredLog = beforeSendLog.Invoke(log);
            }
            catch (Exception e)
            {
                _options.DiagnosticLogger?.LogError(e, "The BeforeSendLog callback threw an exception. The Log will be dropped.");
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
