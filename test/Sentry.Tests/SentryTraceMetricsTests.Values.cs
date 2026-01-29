namespace Sentry.Tests;

public partial class SentryTraceMetricsTests
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

    private static SentryMetric CreateCounter<T>(T value) where T : struct
    {
        return new SentryMetric<T>(DateTimeOffset.MinValue, SentryId.Empty, SentryMetricType.Counter, "sentry_tests.sentry_trace_metrics_tests.counter", value);
    }
}
