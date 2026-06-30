#nullable enable

namespace Sentry.Tests;

public partial class SentryMetricEmitterTests
{
    [Theory]
    [InlineData(SentryMetricType.Counter)]
    [InlineData(SentryMetricType.Gauge)]
    [InlineData(SentryMetricType.Distribution)]
    public void Emit_WithAttributes_Enumerable(SentryMetricType type)
    {
        SentryMetric? captured = null;
        _fixture.Options.SetBeforeSendMetric(SentryMetric? (SentryMetric metric) =>
        {
            captured = metric;
            return null;
        });
        var metrics = _fixture.GetSut();

        IEnumerable<KeyValuePair<string, object>>? attributes = [new KeyValuePair<string, object>("attribute.key", "attribute-value")];
        metrics.Emit<int>(type, 1, attributes);

        captured.Should().NotBeNull();
        captured.Attributes.ShouldContain("attribute.key", "attribute-value");
    }

    [Theory]
    [InlineData(SentryMetricType.Counter)]
    [InlineData(SentryMetricType.Gauge)]
    [InlineData(SentryMetricType.Distribution)]
    public void Emit_WithAttributes_Span(SentryMetricType type)
    {
        SentryMetric? captured = null;
        _fixture.Options.SetBeforeSendMetric(SentryMetric? (SentryMetric metric) =>
        {
            captured = metric;
            return null;
        });
        var metrics = _fixture.GetSut();

        ReadOnlySpan<KeyValuePair<string, object>> attributes = [new KeyValuePair<string, object>("attribute.key", "attribute-value")];
        metrics.Emit<int>(type, 1, attributes);

        captured.Should().NotBeNull();
        captured.Attributes.ShouldContain("attribute.key", "attribute-value");
    }

#if NET6_0_OR_GREATER
    [Theory]
    [InlineData(SentryMetricType.Counter)]
    [InlineData(SentryMetricType.Gauge)]
    [InlineData(SentryMetricType.Distribution)]
    public void Emit_WithAttributes_TagList(SentryMetricType type)
    {
        SentryMetric? captured = null;
        _fixture.Options.SetBeforeSendMetric(SentryMetric? (SentryMetric metric) =>
        {
            captured = metric;
            return null;
        });
        var metrics = _fixture.GetSut();

        TagList attributes = new() { { "attribute.key", "attribute-value" } };
        metrics.Emit<int>(type, 1, in attributes);

        captured.Should().NotBeNull();
        captured.Attributes.ShouldContain("attribute.key", "attribute-value");
    }
#endif
}

[Obsolete(SentryMetricEmitter.ObsoleteStringUnitForwardCompatibility)]
file static class SentryMetricEmitterExtensions
{
    public static void Emit<T>(this SentryMetricEmitter metrics, SentryMetricType type, T value, IEnumerable<KeyValuePair<string, object>>? attributes) where T : struct
    {
        switch (type)
        {
            case SentryMetricType.Counter:
                metrics.EmitCounter<T>("sentry_tests.sentry_trace_metrics_tests.counter", value, attributes);
                break;
            case SentryMetricType.Gauge:
                metrics.EmitGauge<T>("sentry_tests.sentry_trace_metrics_tests.counter", value, "measurement_unit", attributes);
                break;
            case SentryMetricType.Distribution:
                metrics.EmitDistribution<T>("sentry_tests.sentry_trace_metrics_tests.counter", value, "measurement_unit", attributes);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }

    public static void Emit<T>(this SentryMetricEmitter metrics, SentryMetricType type, T value, ReadOnlySpan<KeyValuePair<string, object>> attributes) where T : struct
    {
        switch (type)
        {
            case SentryMetricType.Counter:
                metrics.EmitCounter<T>("sentry_tests.sentry_trace_metrics_tests.counter", value, attributes);
                break;
            case SentryMetricType.Gauge:
                metrics.EmitGauge<T>("sentry_tests.sentry_trace_metrics_tests.counter", value, "measurement_unit", attributes);
                break;
            case SentryMetricType.Distribution:
                metrics.EmitDistribution<T>("sentry_tests.sentry_trace_metrics_tests.counter", value, "measurement_unit", attributes);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }

#if NET6_0_OR_GREATER
    [SuppressMessage("Roslynator", "RCS1242:Do not pass non-read-only struct by read-only reference", Justification = $"Ensure that only readonly instance members of {nameof(TagList)} are invoked, to avoid a defensive copy created by the compiler.")]
    public static void Emit<T>(this SentryMetricEmitter metrics, SentryMetricType type, T value, in TagList attributes) where T : struct
    {
        switch (type)
        {
            case SentryMetricType.Counter:
                metrics.EmitCounter<T>("sentry_tests.sentry_trace_metrics_tests.counter", value, in attributes);
                break;
            case SentryMetricType.Gauge:
                metrics.EmitGauge<T>("sentry_tests.sentry_trace_metrics_tests.counter", value, "measurement_unit", in attributes);
                break;
            case SentryMetricType.Distribution:
                metrics.EmitDistribution<T>("sentry_tests.sentry_trace_metrics_tests.counter", value, "measurement_unit", in attributes);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }
#endif
}
