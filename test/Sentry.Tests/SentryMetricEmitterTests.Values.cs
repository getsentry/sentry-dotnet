#nullable enable

namespace Sentry.Tests;

public partial class SentryMetricEmitterTests
{
    [Fact]
    public void TryGetValue_FromByte_SupportedType()
    {
        var metric = CreateCounter<byte>(1);

        metric.TryGetValue<byte>(out var value).Should().BeTrue();
        value.Should().Be(1);
    }

    [Fact]
    public void TryGetValue_FromInt16_SupportedType()
    {
        var metric = CreateCounter<short>(1);

        metric.TryGetValue<short>(out var value).Should().BeTrue();
        value.Should().Be(1);
    }

    [Fact]
    public void TryGetValue_FromInt32_SupportedType()
    {
        var metric = CreateCounter<int>(1);

        metric.TryGetValue<int>(out var value).Should().BeTrue();
        value.Should().Be(1);
    }

    [Fact]
    public void TryGetValue_FromInt64_SupportedType()
    {
        var metric = CreateCounter<long>(1L);

        metric.TryGetValue<long>(out var value).Should().BeTrue();
        value.Should().Be(1L);
    }

    [Fact]
    public void TryGetValue_FromSingle_SupportedType()
    {
        var metric = CreateCounter<float>(1f);

        metric.TryGetValue<float>(out var value).Should().BeTrue();
        value.Should().Be(1f);
    }

    [Fact]
    public void TryGetValue_FromDouble_SupportedType()
    {
        var metric = CreateCounter<double>(1d);

        metric.TryGetValue<double>(out var value).Should().BeTrue();
        value.Should().Be(1d);
    }

    [Fact]
    public void TryGetValue_FromDecimal_UnsupportedType()
    {
        var metric = CreateCounter<decimal>(1m);

        metric.TryGetValue<byte>(out var @byte).Should().BeFalse();
        @byte.Should().Be(0);
        metric.TryGetValue<short>(out var @short).Should().BeFalse();
        @short.Should().Be(0);
        metric.TryGetValue<int>(out var @int).Should().BeFalse();
        @int.Should().Be(0);
        metric.TryGetValue<long>(out var @long).Should().BeFalse();
        @long.Should().Be(0L);
        metric.TryGetValue<float>(out var @float).Should().BeFalse();
        @float.Should().Be(0f);
        metric.TryGetValue<double>(out var @double).Should().BeFalse();
        @double.Should().Be(0d);
    }

    // see: https://develop.sentry.dev/sdk/telemetry/attributes/#units
    // see: https://getsentry.github.io/relay/relay_metrics/enum.MetricUnit.html
    [Theory]
    [InlineData(MeasurementUnit.Duration.Nanosecond, "nanosecond")]
    [InlineData(MeasurementUnit.Duration.Microsecond, "microsecond")]
    [InlineData(MeasurementUnit.Duration.Millisecond, "millisecond")]
    [InlineData(MeasurementUnit.Duration.Second, "second")]
    [InlineData(MeasurementUnit.Duration.Minute, "minute")]
    [InlineData(MeasurementUnit.Duration.Hour, "hour")]
    [InlineData(MeasurementUnit.Duration.Day, "day")]
    [InlineData(MeasurementUnit.Duration.Week, "week")]
    [InlineData(MeasurementUnit.Information.Bit, "bit")]
    [InlineData(MeasurementUnit.Information.Byte, "byte")]
    [InlineData(MeasurementUnit.Information.Kilobyte, "kilobyte")]
    [InlineData(MeasurementUnit.Information.Kibibyte, "kibibyte")]
    [InlineData(MeasurementUnit.Information.Megabyte, "megabyte")]
    [InlineData(MeasurementUnit.Information.Mebibyte, "mebibyte")]
    [InlineData(MeasurementUnit.Information.Gigabyte, "gigabyte")]
    [InlineData(MeasurementUnit.Information.Gibibyte, "gibibyte")]
    [InlineData(MeasurementUnit.Information.Terabyte, "terabyte")]
    [InlineData(MeasurementUnit.Information.Tebibyte, "tebibyte")]
    [InlineData(MeasurementUnit.Information.Petabyte, "petabyte")]
    [InlineData(MeasurementUnit.Information.Pebibyte, "pebibyte")]
    [InlineData(MeasurementUnit.Information.Exabyte, "exabyte")]
    [InlineData(MeasurementUnit.Information.Exbibyte, "exbibyte")]
    [InlineData(MeasurementUnit.Fraction.Ratio, "ratio")]
    [InlineData(MeasurementUnit.Fraction.Percent, "percent")]
    public void Emit_Unit_MeasurementUnit_Predefined(MeasurementUnit unit, string expected)
    {
        SentryMetric? captured = null;
        _fixture.Options.Experimental.SetBeforeSendMetric(SentryMetric? (SentryMetric metric) =>
        {
            captured = metric;
            return null;
        });
        var metrics = _fixture.GetSut();

        metrics.EmitGauge<int>("sentry_tests.sentry_trace_metrics_tests.gauge", 1, unit);

        captured.Should().NotBeNull();
        captured.Unit.Should().Be(expected);
    }

    [Fact]
    public void Emit_Unit_MeasurementUnit_None()
    {
        SentryMetric? captured = null;
        _fixture.Options.Experimental.SetBeforeSendMetric(SentryMetric? (SentryMetric metric) =>
        {
            captured = metric;
            return null;
        });
        var metrics = _fixture.GetSut();

        metrics.EmitGauge<int>("sentry_tests.sentry_trace_metrics_tests.gauge", 1, MeasurementUnit.None);

        captured.Should().NotBeNull();
        captured.Unit.Should().Be("none");
    }

    [Fact]
    public void Emit_Unit_MeasurementUnit_Custom()
    {
        SentryMetric? captured = null;
        _fixture.Options.Experimental.SetBeforeSendMetric(SentryMetric? (SentryMetric metric) =>
        {
            captured = metric;
            return null;
        });
        var metrics = _fixture.GetSut();

        metrics.EmitGauge<int>("sentry_tests.sentry_trace_metrics_tests.gauge", 1, MeasurementUnit.Custom("custom_unit"));

        captured.Should().NotBeNull();
        captured.Unit.Should().Be("custom_unit");
    }

    [Fact]
    public void Emit_Unit_MeasurementUnit_Empty()
    {
        SentryMetric? captured = null;
        _fixture.Options.Experimental.SetBeforeSendMetric(SentryMetric? (SentryMetric metric) =>
        {
            captured = metric;
            return null;
        });
        var metrics = _fixture.GetSut();

        metrics.EmitGauge<int>("sentry_tests.sentry_trace_metrics_tests.gauge", 1, MeasurementUnit.Custom(""));

        captured.Should().NotBeNull();
        captured.Unit.Should().BeEmpty();
    }

    [Fact]
    public void Emit_Unit_MeasurementUnit_Null()
    {
        SentryMetric? captured = null;
        _fixture.Options.Experimental.SetBeforeSendMetric(SentryMetric? (SentryMetric metric) =>
        {
            captured = metric;
            return null;
        });
        var metrics = _fixture.GetSut();

        metrics.EmitGauge<int>("sentry_tests.sentry_trace_metrics_tests.gauge", 1, MeasurementUnit.Parse(null));

        captured.Should().NotBeNull();
        captured.Unit.Should().BeNull();
    }

    [Fact]
    public void Emit_Unit_MeasurementUnit_Default()
    {
        SentryMetric? captured = null;
        _fixture.Options.Experimental.SetBeforeSendMetric(SentryMetric? (SentryMetric metric) =>
        {
            captured = metric;
            return null;
        });
        var metrics = _fixture.GetSut();

        metrics.EmitGauge<int>("sentry_tests.sentry_trace_metrics_tests.gauge", 1, default(MeasurementUnit));

        captured.Should().NotBeNull();
        captured.Unit.Should().BeNull();
    }

    [Fact]
    [Obsolete(SentryMetricEmitter.ObsoleteStringUnitForwardCompatibility)]
    public void Emit_Unit_String_Custom()
    {
        SentryMetric? captured = null;
        _fixture.Options.Experimental.SetBeforeSendMetric(SentryMetric? (SentryMetric metric) =>
        {
            captured = metric;
            return null;
        });
        var metrics = _fixture.GetSut();

        metrics.EmitDistribution<int>("sentry_tests.sentry_trace_metrics_tests.distribution", 1, "custom_unit");

        captured.Should().NotBeNull();
        captured.Unit.Should().Be("custom_unit");
    }

    [Fact]
    [Obsolete(SentryMetricEmitter.ObsoleteStringUnitForwardCompatibility)]
    public void Emit_Unit_String_Empty()
    {
        SentryMetric? captured = null;
        _fixture.Options.Experimental.SetBeforeSendMetric(SentryMetric? (SentryMetric metric) =>
        {
            captured = metric;
            return null;
        });
        var metrics = _fixture.GetSut();

        metrics.EmitDistribution<int>("sentry_tests.sentry_trace_metrics_tests.distribution", 1, "");

        captured.Should().NotBeNull();
        captured.Unit.Should().BeEmpty();
    }

    [Fact]
    [Obsolete(SentryMetricEmitter.ObsoleteStringUnitForwardCompatibility)]
    public void Emit_Unit_String_Null()
    {
        SentryMetric? captured = null;
        _fixture.Options.Experimental.SetBeforeSendMetric(SentryMetric? (SentryMetric metric) =>
        {
            captured = metric;
            return null;
        });
        var metrics = _fixture.GetSut();

        metrics.EmitDistribution<int>("sentry_tests.sentry_trace_metrics_tests.distribution", 1, (string?)null);

        captured.Should().NotBeNull();
        captured.Unit.Should().BeNull();
    }

    private static SentryMetric CreateCounter<T>(T value) where T : struct
    {
        return new SentryMetric<T>(DateTimeOffset.MinValue, SentryId.Empty, SentryMetricType.Counter, "sentry_tests.sentry_trace_metrics_tests.counter", value);
    }
}
