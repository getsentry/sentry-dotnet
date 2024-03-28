#if NET8_0_OR_GREATER
using System.Diagnostics.Metrics;

namespace Sentry.Internal;

internal class SystemDiagnosticsMetricsListener : IDisposable
{
    private readonly Lazy<IMetricAggregator> _metricsAggregator;
    private IMetricAggregator MetricsAggregator => _metricsAggregator.Value;
    private static SystemDiagnosticsMetricsListener? DefaultListener;

    internal readonly MeterListener _sentryListener = new();

    private SystemDiagnosticsMetricsListener(MetricsOptions metricsOptions)
        : this(metricsOptions, () => SentrySdk.Metrics)
    {
    }

    /// <summary>
    /// Overload for testing purposes - allows us to supply a mock IMetricAggregator
    /// </summary>
    internal SystemDiagnosticsMetricsListener(MetricsOptions metricsOptions, Func<IMetricAggregator> metricsAggregatorResolver)
    {
        _metricsAggregator = new Lazy<IMetricAggregator>(metricsAggregatorResolver);
        _sentryListener.InstrumentPublished = (instrument, listener) =>
        {
            if (metricsOptions.CaptureSystemDiagnosticsMeters.ContainsMatch(instrument.Meter.Name)
                || metricsOptions.CaptureSystemDiagnosticsInstruments.ContainsMatch(instrument.Name))
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        _sentryListener.SetMeasurementEventCallback<byte>(RecordMeasurement);
        _sentryListener.SetMeasurementEventCallback<int>(RecordMeasurement);
        _sentryListener.SetMeasurementEventCallback<float>(RecordMeasurement);
        _sentryListener.SetMeasurementEventCallback<decimal>(RecordMeasurement);
        _sentryListener.SetMeasurementEventCallback<short>(RecordMeasurement);
        _sentryListener.SetMeasurementEventCallback<long>(RecordMeasurement);
        _sentryListener.SetMeasurementEventCallback<double>(RecordMeasurement);
        _sentryListener.Start();
    }

    internal static void InitializeDefaultListener(MetricsOptions metricsOptions)
    {
        var oldListener = Interlocked.Exchange(
            ref DefaultListener,
            new SystemDiagnosticsMetricsListener(metricsOptions)
            );
        oldListener?.Dispose();
    }

    internal void RecordMeasurement<T>(
        Instrument instrument,
        T measurement,
        ReadOnlySpan<KeyValuePair<string, object?>> tags,
        object? _)
        where T : struct, IConvertible
    {
        var unit = MeasurementUnit.Parse(instrument.Unit);
        var tagDict = tags.ToImmutableArray().ToImmutableDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value?.ToString() ?? string.Empty
        );
        var doubleMeasurement = Convert.ToDouble(measurement);
        switch (instrument)
        {
            case Counter<T>:
            case UpDownCounter<T>:
            case ObservableCounter<T>:
            case ObservableUpDownCounter<T>:
                MetricsAggregator.Increment(instrument.Name, doubleMeasurement, unit, tagDict);
                break;
            case Histogram<T>:
                MetricsAggregator.Distribution(instrument.Name, doubleMeasurement, unit, tagDict);
                break;
            case ObservableGauge<T>:
                MetricsAggregator.Gauge(instrument.Name, doubleMeasurement, unit, tagDict);
                break;
        }
    }

    public void Dispose()
    {
        _sentryListener.Dispose();
    }
}
#endif
