using Sentry.Extensibility;
using Sentry.Infrastructure;

namespace Sentry.Internal;

internal sealed class DefaultSentryStructuredLogger : SentryStructuredLogger
{
    private readonly IHub _hub;
    private readonly SentryOptions _options;
    private readonly ISystemClock _clock;

    private readonly StructuredLogBatchProcessor _batchProcessor;

    internal DefaultSentryStructuredLogger(IHub hub, SentryOptions options, ISystemClock clock, int batchCount, TimeSpan batchInterval)
    {
        Debug.Assert(hub.IsEnabled);
        Debug.Assert(options is { Experimental.EnableLogs: true });

        _hub = hub;
        _options = options;
        _clock = clock;

        _batchProcessor = new StructuredLogBatchProcessor(hub, batchCount, batchInterval, _options.ClientReportRecorder, _options.DiagnosticLogger);
    }

    /// <inheritdoc />
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

        ImmutableArray<KeyValuePair<string, object>> @params = default;
        if (parameters is not null && parameters.Length != 0)
        {
            var builder = ImmutableArray.CreateBuilder<KeyValuePair<string, object>>(parameters.Length);
            for (var index = 0; index < parameters.Length; index++)
            {
                builder.Add(new KeyValuePair<string, object>(index.ToString(), parameters[index]));
            }
            @params = builder.DrainToImmutable();
        }

        SentryLog log = new(timestamp, traceHeader.TraceId, level, message)
        {
            Template = template,
            Parameters = @params,
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

        CaptureLog(log);
    }

    /// <inheritdoc />
    protected internal override void CaptureLog(SentryLog log)
    {
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

    /// <inheritdoc />
    protected internal override void Flush()
    {
        _batchProcessor.Flush();
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _batchProcessor.Dispose();
        }

        base.Dispose(disposing);
    }
}
