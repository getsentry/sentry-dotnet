using Sentry.Extensibility;
using Sentry.Infrastructure;
using Sentry.Protocol.Envelopes;

namespace Sentry.Internal;

internal sealed class DefaultSentryStructuredLogger : SentryStructuredLogger
{
    private readonly IHub _hub;
    private readonly SentryOptions _options;
    private readonly ISystemClock _clock;

    internal DefaultSentryStructuredLogger(IHub hub, SentryOptions options, ISystemClock clock)
    {
        Debug.Assert(options is { Experimental.EnableLogs: true });

        _hub = hub;
        _options = options;
        _clock = clock;
    }

    private protected override void CaptureLog(SentryLogLevel level, string template, object[]? parameters, Action<SentryLog>? configureLog)
    {
        var timestamp = _clock.GetUtcNow();
        var traceHeader = _hub.GetTraceHeader() ?? SentryTraceHeader.Empty;

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

        SentryLog log = new(timestamp, traceHeader.TraceId, level, message)
        {
            Template = template,
            Parameters = ImmutableArray.Create(parameters),
            ParentSpanId = traceHeader.SpanId,
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
}
