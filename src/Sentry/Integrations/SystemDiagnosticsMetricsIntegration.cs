#if NET8_0_OR_GREATER
using System.Diagnostics.Metrics;
using Sentry.Extensibility;

namespace Sentry.Integrations;

internal class SystemDiagnosticsMetricsIntegration : ISdkIntegration
{
    private static MeterListener? SentryListener;

    public void Register(IHub hub, SentryOptions options)
    {
        var captureInstruments = options.ExperimentalMetrics?.CaptureInstruments;
        if (captureInstruments is not { Count: > 0 })
        {
            options.LogInfo("System.Diagnostics.Metrics Integration is disabled because no listeners configured.");
            return;
        }

        InitializeMeterListener(captureInstruments);
    }

    private static void InitializeMeterListener(IList<SubstringOrRegexPattern> captureInstruments)
    {
        var sentryListener = new MeterListener();
        sentryListener.InstrumentPublished = (instrument, listener) =>
        {
            if (captureInstruments!.Any(x => x.IsMatch(instrument.Name)))
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        sentryListener.SetMeasurementEventCallback<byte>(RecordMeasurement);
        sentryListener.SetMeasurementEventCallback<int>(RecordMeasurement);
        sentryListener.SetMeasurementEventCallback<float>(RecordMeasurement);
        sentryListener.SetMeasurementEventCallback<decimal>(RecordMeasurement);
        sentryListener.SetMeasurementEventCallback<short>(RecordMeasurement);
        sentryListener.SetMeasurementEventCallback<long>(RecordMeasurement);
        sentryListener.SetMeasurementEventCallback<double>(RecordMeasurement);
        sentryListener.Start();
        SentryListener = sentryListener;
    }

    private static void RecordMeasurement<T>(
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
                SentrySdk.Metrics.Increment(instrument.Name, doubleMeasurement, unit, tagDict);
                break;
            case Histogram<T>:
                SentrySdk.Metrics.Distribution(instrument.Name, doubleMeasurement, unit, tagDict);
                break;
            case ObservableGauge<T>:
                SentrySdk.Metrics.Gauge(instrument.Name, doubleMeasurement, unit, tagDict);
                break;
        }
    }
}
#endif
