#if NET8_0_OR_GREATER
using System.Diagnostics.Metrics;

namespace Sentry.Internal;

internal class SystemDiagnosticsMetricsListener : IDisposable
{
    private readonly IMetricAggregator _metricsAggregator;
    internal static SystemDiagnosticsMetricsListener? DefaultListener;

    private readonly MeterListener _sentryListener = new ();

    public SystemDiagnosticsMetricsListener(IEnumerable<SubstringOrRegexPattern> captureInstruments)
        : this(captureInstruments, SentrySdk.Metrics)
    {
    }

    /// <summary>
    /// Overload for testing purposes - allows us to supply a mock IMetricAggregator
    /// </summary>
    internal SystemDiagnosticsMetricsListener(IEnumerable<SubstringOrRegexPattern> captureInstruments, IMetricAggregator metricsAggregator)
    {
        _metricsAggregator = metricsAggregator;
        _sentryListener.InstrumentPublished = (instrument, listener) =>
        {
            if (captureInstruments!.Any(x => x.IsMatch(instrument.Name)))
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

    internal static void InitializeDefaultListener(IEnumerable<SubstringOrRegexPattern> captureInstruments)
    {
        var oldListener = Interlocked.Exchange(
            ref DefaultListener,
            new SystemDiagnosticsMetricsListener(captureInstruments)
            );
        oldListener?.Dispose();
    }

    internal void RecordMeasurement<T>(
        Instrument instrument,
        T measurement,
        ReadOnlySpan<KeyValuePair<string, object?>> tags,
        object? _)
        where T: struct
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
                _metricsAggregator.Increment(instrument.Name, doubleMeasurement, unit, tagDict);
                break;
            case Histogram<T>:
                _metricsAggregator.Distribution(instrument.Name, doubleMeasurement, unit, tagDict);
                break;
            case ObservableGauge<T>:
                _metricsAggregator.Gauge(instrument.Name, doubleMeasurement, unit, tagDict);
                break;
        }
    }

    public void Dispose()
    {
        _sentryListener.Dispose();
    }
}
#endif
