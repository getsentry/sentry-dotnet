#if NET8_0_OR_GREATER
using System.Diagnostics.Metrics;
using Sentry.Extensibility;

namespace Sentry.Integrations;

internal class SystemDiagnosticsMetricsIntegration : ISdkIntegration
{
    private static readonly Lazy<MeterListener> SentryMeterListener = new(() => new MeterListener());

    public void Register(IHub hub, SentryOptions options)
    {
        var listeners = options.ExperimentalMetrics?.SystemDiagnosticsMetricsListeners;
        if (listeners is not { Count: > 0 })
        {
            options.LogInfo("System.Diagnostics.Metrics Integration is disabled because no listeners configured.");
            return;
        }

        var sentryListener = SentryMeterListener.Value;
        sentryListener.InstrumentPublished = (instrument, listener) =>
        {
            if (listeners!.Any(x => x.IsMatch(instrument.Meter.Name)))
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        sentryListener.SetMeasurementEventCallback<int>(RecordIntMeasurement);
        sentryListener.SetMeasurementEventCallback<double>(RecordDoubleMeasurement);
        sentryListener.Start();
    }

    private static void RecordIntMeasurement(
        Instrument instrument,
        int measurement,
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
            case Counter<int>:
                SentrySdk.Metrics.Increment(instrument.Name, measurement, unit, tagDict);
                break;
            case Histogram<int>:
                SentrySdk.Metrics.Distribution(instrument.Name, measurement, unit, tagDict);
                break;
            case ObservableGauge<int>:
                SentrySdk.Metrics.Gauge(instrument.Name, measurement, unit, tagDict);
                break;
        }
    }
}
#endif
