#if NET8_0_OR_GREATER
using System.Diagnostics.Metrics;
using Sentry.Extensibility;

namespace Sentry.Integrations;

internal class SystemDiagnosticsMetricsIntegration : ISdkIntegration
{
    private static readonly Lazy<MeterListener> SentryMeterListener = new(() => new MeterListener());

    public void Register(IHub hub, SentryOptions options)
    {
        var listeners = options.ExperimentalMetrics?.CaptureInstruments;
        if (listeners is not { Count: > 0 })
        {
            options.LogInfo("System.Diagnostics.Metrics Integration is disabled because no listeners configured.");
            return;
        }

        var sentryListener = SentryMeterListener.Value;
        sentryListener.InstrumentPublished = (instrument, listener) =>
        {
            // TODO: It might be convenient to also be able to match against the Meter.Name as it matches a whole family of metrics - maybe as a separate option though
            if (listeners!.Any(x => x.IsMatch(instrument.Name)))
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        sentryListener.SetMeasurementEventCallback<byte>(RecordByteMeasurement);
        sentryListener.SetMeasurementEventCallback<int>(RecordIntMeasurement);
        sentryListener.SetMeasurementEventCallback<float>(RecordFloatMeasurement);
        sentryListener.SetMeasurementEventCallback<decimal>(RecordDecimalMeasurement);
        sentryListener.SetMeasurementEventCallback<short>(RecordShortMeasurement);
        sentryListener.SetMeasurementEventCallback<long>(RecordLongMeasurement);
        sentryListener.SetMeasurementEventCallback<double>(RecordDoubleMeasurement);
        sentryListener.Start();
    }

    private static void RecordByteMeasurement(
        Instrument instrument,
        byte measurement,
        ReadOnlySpan<KeyValuePair<string, object?>> tags,
        object? state) => RecordDoubleMeasurement(instrument, measurement, tags, state);

    private static void RecordIntMeasurement(
        Instrument instrument,
        int measurement,
        ReadOnlySpan<KeyValuePair<string, object?>> tags,
        object? state) => RecordDoubleMeasurement(instrument, measurement, tags, state);

    private static void RecordFloatMeasurement(
        Instrument instrument,
        float measurement,
        ReadOnlySpan<KeyValuePair<string, object?>> tags,
        object? state) => RecordDoubleMeasurement(instrument, measurement, tags, state);

    private static void RecordDecimalMeasurement(
        Instrument instrument,
        decimal measurement,
        ReadOnlySpan<KeyValuePair<string, object?>> tags,
        object? state) => RecordDoubleMeasurement(instrument, (double)measurement, tags, state);

    private static void RecordShortMeasurement(
        Instrument instrument,
        short measurement,
        ReadOnlySpan<KeyValuePair<string, object?>> tags,
        object? state) => RecordDoubleMeasurement(instrument, measurement, tags, state);

    private static void RecordLongMeasurement(
        Instrument instrument,
        long measurement,
        ReadOnlySpan<KeyValuePair<string, object?>> tags,
        object? state) => RecordDoubleMeasurement(instrument, measurement, tags, state);

    private static void RecordDoubleMeasurement(
        Instrument instrument,
        double measurement,
        ReadOnlySpan<KeyValuePair<string, object?>> tags,
        object? _)
    {
        var unit = MeasurementUnit.Parse(instrument.Unit);
        var tagDict = tags.ToImmutableArray().ToImmutableDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value?.ToString() ?? string.Empty
        );
        switch (instrument)
        {
            case Counter<byte>:
            case Counter<int>:
            case Counter<float>:
            case Counter<decimal>:
            case Counter<short>:
            case Counter<long>:
            case Counter<double>:
                SentrySdk.Metrics.Increment(instrument.Name, measurement, unit, tagDict);
                break;
            case Histogram<byte>:
            case Histogram<int>:
            case Histogram<float>:
            case Histogram<decimal>:
            case Histogram<short>:
            case Histogram<long>:
            case Histogram<double>:
                SentrySdk.Metrics.Distribution(instrument.Name, measurement, unit, tagDict);
                break;
            case ObservableGauge<byte>:
            case ObservableGauge<int>:
            case ObservableGauge<float>:
            case ObservableGauge<decimal>:
            case ObservableGauge<short>:
            case ObservableGauge<long>:
            case ObservableGauge<double>:
                SentrySdk.Metrics.Gauge(instrument.Name, measurement, unit, tagDict);
                break;
        }
    }
}
#endif
