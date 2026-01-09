using Sentry.Extensibility;
using Sentry.Infrastructure;
using Sentry.Protocol;

namespace Sentry.Internal;

internal sealed class DefaultSentryTraceMetrics : SentryTraceMetrics, IDisposable
{
    private readonly IHub _hub;
    private readonly SentryOptions _options;
    private readonly ISystemClock _clock;

    private readonly BatchProcessor<ISentryMetric> _batchProcessor;

    internal DefaultSentryTraceMetrics(IHub hub, SentryOptions options, ISystemClock clock, int batchCount, TimeSpan batchInterval)
    {
        Debug.Assert(hub.IsEnabled);
        Debug.Assert(options.Experimental is { EnableMetrics: true });

        _hub = hub;
        _options = options;
        _clock = clock;

        _batchProcessor = new BatchProcessor<ISentryMetric>(hub, batchCount, batchInterval, TraceMetric.Capture, _options.ClientReportRecorder, _options.DiagnosticLogger);
    }

    /// <inheritdoc />
    private protected override void CaptureMetric<T>(SentryMetricType type, string name, T value, string? unit, IEnumerable<KeyValuePair<string, object>>? attributes, Scope? scope) where T : struct
    {
        var metric = SentryMetric.Create(_hub, _clock, type, name, value, unit, attributes, scope);
        CaptureMetric(metric);
    }

    /// <inheritdoc />
    private protected override void CaptureMetric<T>(SentryMetricType type, string name, T value, string? unit, ReadOnlySpan<KeyValuePair<string, object>> attributes, Scope? scope) where T : struct
    {
        var metric = SentryMetric.Create(_hub, _clock, type, name, value, unit, attributes, scope);
        CaptureMetric(metric);
    }

    /// <inheritdoc />
    protected internal override void CaptureMetric<T>(SentryMetric<T> metric) where T : struct
    {
        var configuredMetric = metric;

        if (_options.Experimental.BeforeSendMetricInternal is { } beforeSendMetric)
        {
            try
            {
                configuredMetric = beforeSendMetric.Invoke(metric);
            }
            catch (Exception e)
            {
                _options.DiagnosticLogger?.LogError(e, "The BeforeSendMetric callback threw an exception. The Metric will be dropped.");
                return;
            }
        }

        if (configuredMetric is not null)
        {
            _batchProcessor.Enqueue(configuredMetric);
        }
    }

    /// <inheritdoc />
    protected internal override void Flush()
    {
        _batchProcessor.Flush();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _batchProcessor.Dispose();
    }
}
