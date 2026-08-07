#nullable enable

using BenchmarkDotNet.Attributes;
using Sentry.Extensibility;
using Sentry.Internal;
using Sentry.Testing;

namespace Sentry.Benchmarks;

public class SentryMetricEmitterBenchmarks
{
    private Hub _hub = null!;
    private SentryMetricEmitter _metrics = null!;

    private SentryMetric? _lastMetric;

    [GlobalSetup]
    public void Setup()
    {
        SentryOptions options = new()
        {
            Dsn = DsnSamples.ValidDsn,
            EnableMetrics = true,
        };
        options.SetBeforeSendMetric((SentryMetric metric) =>
        {
            _lastMetric = metric;
            return null;
        });

        MockClock clock = new(new DateTimeOffset(2025, 04, 22, 14, 51, 00, 789, TimeSpan.FromHours(2)));

        _hub = new Hub(options, DisabledHub.Instance);
        _metrics = SentryMetricEmitter.Create(_hub, options, clock);
    }

    [Benchmark]
    public void EmitWithoutAttributes()
    {
        _metrics.EmitGauge("sentry_benchmarks.sentry_trace_metrics_tests.gauge", 1);
    }

    [Benchmark]
    public void EmitWithAttributes_Enumerable()
    {
        IEnumerable<KeyValuePair<string, object>> attributes = new List<KeyValuePair<string, object>>(1)
        {
            KeyValuePair.Create<string, object>("attribute.key", "attribute-value"),
        };
        _metrics.EmitGauge("sentry_benchmarks.sentry_trace_metrics_tests.gauge", 1, MeasurementUnit.Information.Bit, attributes);
    }

    [Benchmark]
    public void EmitWithAttributes_Span()
    {
        ReadOnlySpan<KeyValuePair<string, object>> attributes =
        [
            KeyValuePair.Create<string, object>("attribute.key", "attribute-value"),
        ];
        _metrics.EmitGauge("sentry_benchmarks.sentry_trace_metrics_tests.gauge", 1, MeasurementUnit.Information.Bit, attributes);
    }

    [Benchmark]
    public void EmitWithAttributes_TagList()
    {
        TagList attributes = new()
        {
            { "attribute.key", "attribute-value" },
        };
        _metrics.EmitGauge("sentry_benchmarks.sentry_trace_metrics_tests.gauge", 1, MeasurementUnit.Information.Bit, in attributes);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        (_metrics as IDisposable)?.Dispose();
        _hub.Dispose();

        if (_lastMetric is null)
        {
            throw new InvalidOperationException("Last Metric is null");
        }
    }
}
