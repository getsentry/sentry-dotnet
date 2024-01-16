#if NET8_0_OR_GREATER
using System.Diagnostics.Metrics;
using Sentry.Extensibility;

namespace Sentry.Integrations;

internal class SystemDiagnosticsMetricsIntegration : ISdkIntegration
{
    private static MeterListener? SentryMeterListener;
    private static readonly object InitLock = new();

    public void Register(IHub hub, SentryOptions options)
    {
        var listeners = options.ExperimentalMetrics?.SystemDiagnosticsMetricsListeners;
        if (listeners is not { Count: > 0 })
        {
            options.Log(SentryLevel.Info,
                "System.Diagnostics.Metrics Integration is disabled because no listeners configured.");
            return;
        }

        if (SentryMeterListener is not null)
        {
            options.Log(SentryLevel.Info,
                "System.Diagnostics.Metrics Integration has already been registered.");
            return;
        }

        lock (InitLock)
        {
            if (SentryMeterListener is not null)
            {
                options.Log(SentryLevel.Info,
                    "System.Diagnostics.Metrics Integration has already been registered.");
                return;
            }

            SentryMeterListener = new MeterListener();
            SentryMeterListener.InstrumentPublished = (instrument, listener) =>
            {
                if (listeners!.Any(x => x.IsMatch(instrument.Meter.Name)))
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            };
            SentryMeterListener.SetMeasurementEventCallback<int>(RecordIntMeasurement);
            SentryMeterListener.SetMeasurementEventCallback<double>(RecordDoubleMeasurement);
            SentryMeterListener.Start();
        }
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
