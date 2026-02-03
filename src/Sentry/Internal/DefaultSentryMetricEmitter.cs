using Sentry.Extensibility;
using Sentry.Infrastructure;

namespace Sentry.Internal;

internal sealed class DefaultSentryMetricEmitter : SentryMetricEmitter, IDisposable
{
    private readonly IHub _hub;
    private readonly SentryOptions _options;
    private readonly ISystemClock _clock;

    private readonly BatchProcessor<SentryMetric> _batchProcessor;

    internal DefaultSentryMetricEmitter(IHub hub, SentryOptions options, ISystemClock clock, int batchCount, TimeSpan batchInterval)
    {
        Debug.Assert(hub.IsEnabled);
        Debug.Assert(options.Experimental is { EnableMetrics: true });

        _hub = hub;
        _options = options;
        _clock = clock;

        _batchProcessor = new SentryMetricBatchProcessor(hub, batchCount, batchInterval, _options.ClientReportRecorder, _options.DiagnosticLogger);
    }

    /// <inheritdoc />
    private protected override void CaptureMetric<T>(SentryMetricType type, string name, T value, string? unit, IEnumerable<KeyValuePair<string, object>>? attributes, Scope? scope) where T : struct
    {
        if (!SentryMetric.IsSupported(typeof(T)))
        {
            _options.DiagnosticLogger?.LogWarning("{0} is unsupported type for Sentry Metrics. The only supported types are byte, short, int, long, float, and double.", typeof(T));
            return;
        }

        if (string.IsNullOrEmpty(name))
        {
            _options.DiagnosticLogger?.LogWarning("Name of metrics cannot be null or empty. Metric-Type: {0}; Value-Type: {1}", type.ToString(), typeof(T));
            return;
        }

        var metric = SentryMetric.Create(_hub, _options, _clock, type, name, value, unit, attributes, scope);
        CaptureMetric(metric);
    }

    /// <inheritdoc />
    private protected override void CaptureMetric<T>(SentryMetricType type, string name, T value, string? unit, ReadOnlySpan<KeyValuePair<string, object>> attributes, Scope? scope) where T : struct
    {
        if (!SentryMetric.IsSupported(typeof(T)))
        {
            _options.DiagnosticLogger?.LogWarning("{0} is unsupported type for Sentry Metrics. The only supported types are byte, short, int, long, float, and double.", typeof(T));
            return;
        }

        if (string.IsNullOrEmpty(name))
        {
            _options.DiagnosticLogger?.LogWarning("Name of metrics cannot be null or empty. Metric-Type: {0}; Value-Type: {1}", type.ToString(), typeof(T));
            return;
        }

        var metric = SentryMetric.Create(_hub, _options, _clock, type, name, value, unit, attributes, scope);
        CaptureMetric(metric);
    }

    /// <inheritdoc />
    private protected override void CaptureMetric<T>(SentryMetric<T> metric) where T : struct
    {
        Debug.Assert(SentryMetric.IsSupported(typeof(T)));
        Debug.Assert(!string.IsNullOrEmpty(metric.Name));

        SentryMetric? configuredMetric = metric;

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
