using Sentry.Extensibility;
using Sentry.Infrastructure;

namespace Sentry.Internal;

internal sealed class DefaultSentryStructuredLogger : SentryStructuredLogger
{
    private readonly IHub _hub;
    private readonly SentryOptions _options;
    private readonly ISystemClock _clock;

    private readonly BatchProcessor _batchProcessor;

    internal DefaultSentryStructuredLogger(IHub hub, SentryOptions options, ISystemClock clock)
    {
        Debug.Assert(options is { Experimental.EnableLogs: true });

        _hub = hub;
        _options = options;
        _clock = clock;

        _batchProcessor = new BatchProcessor(hub, ClampBatchCount(options.Experimental.InternalBatchSize), ClampBatchInterval(options.Experimental.InternalBatchTimeout), clock, _options.ClientReportRecorder, _options.DiagnosticLogger);
    }

    private static int ClampBatchCount(int batchCount)
    {
        return batchCount <= 0
            ? 1
            : batchCount > 1_000_000
                ? 1_000_000
                : batchCount;
    }

    private static TimeSpan ClampBatchInterval(TimeSpan batchInterval)
    {
        return batchInterval.TotalMilliseconds is <= 0 or > int.MaxValue
            ? TimeSpan.FromMilliseconds(int.MaxValue)
            : batchInterval;
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

        var scope = _hub.GetScope();
        log.SetDefaultAttributes(_options, scope?.Sdk ?? SdkVersion.Instance);

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
            _batchProcessor.Enqueue(configuredLog);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _batchProcessor.Dispose();
        }

        base.Dispose(disposing);
    }
}
