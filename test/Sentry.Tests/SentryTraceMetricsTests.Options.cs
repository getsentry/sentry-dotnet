#nullable enable

namespace Sentry.Tests;

public partial class SentryTraceMetricsTests
{
    [Fact]
    public void EnableMetrics_Default_True()
    {
        var options = new SentryOptions();

        options.Experimental.EnableMetrics.Should().BeTrue();
    }

    [Fact]
    public void BeforeSendMetric_Default_Null()
    {
        var options = new SentryOptions();

        options.Experimental.BeforeSendMetricInternal.Should().BeNull();
    }

    [Fact]
    public void BeforeSendMetric_Set_NotNull()
    {
        _fixture.Options.Experimental.SetBeforeSendMetric<int>(Callback<int>.Nop);

        _fixture.Options.Experimental.BeforeSendMetricInternal.Should().NotBeNull();
    }

    [Fact]
    public void BeforeSendMetric_SetByte_InvokeDelegate()
    {
        _fixture.Options.Experimental.SetBeforeSendMetric<byte>(static metric =>
        {
            metric.SetAttribute(nameof(Byte), nameof(Byte));
            return metric;
        });

        var metric = CreateCounter<byte>(1);
        _fixture.Options.Experimental.BeforeSendMetricInternal!.Invoke(metric);

        metric.TryGetAttribute<string>(nameof(Byte), out var value).Should().BeTrue();
        value.Should().Be(nameof(Byte));
    }

    [Fact]
    public void BeforeSendMetric_SetInt16_InvokeDelegate()
    {
        _fixture.Options.Experimental.SetBeforeSendMetric<short>(static metric =>
        {
            metric.SetAttribute(nameof(Int16), nameof(Int16));
            return metric;
        });

        var metric = CreateCounter<short>(1);
        _fixture.Options.Experimental.BeforeSendMetricInternal!.Invoke(metric);

        metric.TryGetAttribute<string>(nameof(Int16), out var value).Should().BeTrue();
        value.Should().Be(nameof(Int16));
    }

    [Fact]
    public void BeforeSendMetric_SetInt32_InvokeDelegate()
    {
        _fixture.Options.Experimental.SetBeforeSendMetric<int>(static metric =>
        {
            metric.SetAttribute(nameof(Int32), nameof(Int32));
            return metric;
        });

        var metric = CreateCounter<int>(1);
        _fixture.Options.Experimental.BeforeSendMetricInternal!.Invoke(metric);

        metric.TryGetAttribute<string>(nameof(Int32), out var value).Should().BeTrue();
        value.Should().Be(nameof(Int32));
    }

    [Fact]
    public void BeforeSendMetric_SetInt64_InvokeDelegate()
    {
        _fixture.Options.Experimental.SetBeforeSendMetric<long>(static metric =>
        {
            metric.SetAttribute(nameof(Int64), nameof(Int64));
            return metric;
        });

        var metric = CreateCounter<long>(1L);
        _fixture.Options.Experimental.BeforeSendMetricInternal!.Invoke(metric);

        metric.TryGetAttribute<string>(nameof(Int64), out var value).Should().BeTrue();
        value.Should().Be(nameof(Int64));
    }

    [Fact]
    public void BeforeSendMetric_SetSingle_InvokeDelegate()
    {
        _fixture.Options.Experimental.SetBeforeSendMetric<float>(static metric =>
        {
            metric.SetAttribute(nameof(Single), nameof(Single));
            return metric;
        });

        var metric = CreateCounter<float>(1f);
        _fixture.Options.Experimental.BeforeSendMetricInternal!.Invoke(metric);

        metric.TryGetAttribute<string>(nameof(Single), out var value).Should().BeTrue();
        value.Should().Be(nameof(Single));
    }

    [Fact]
    public void BeforeSendMetric_SetDouble_InvokeDelegate()
    {
        _fixture.Options.Experimental.SetBeforeSendMetric<double>(static metric =>
        {
            metric.SetAttribute(nameof(Double), nameof(Double));
            return metric;
        });

        var metric = CreateCounter<double>(1d);
        _fixture.Options.Experimental.BeforeSendMetricInternal!.Invoke(metric);

        metric.TryGetAttribute<string>(nameof(Double), out var value).Should().BeTrue();
        value.Should().Be(nameof(Double));
    }

    [Fact]
    public void BeforeSendMetric_SetDecimal_UnsupportedType()
    {
        _fixture.Options.Experimental.SetBeforeSendMetric<decimal>(static metric =>
        {
            metric.SetAttribute(nameof(Decimal), nameof(Decimal));
            return metric;
        });

        _fixture.Options.Experimental.BeforeSendMetricInternal.Should().NotBeNull();

        var entry = _fixture.DiagnosticLogger.Dequeue();
        entry.Level.Should().Be(SentryLevel.Warning);
        entry.Message.Should().Be("{0} is unsupported type for Sentry Metrics. The only supported types are byte, short, int, long, float, and double.");
        entry.Exception.Should().BeNull();
        entry.Args.Should().BeEquivalentTo([typeof(decimal)]);
    }

    [Fact]
    public void BeforeSendMetric_SetNull_NoOp()
    {
        _fixture.Options.Experimental.SetBeforeSendMetric<int>(null!);

        var metric = CreateCounter<int>(1);
        _fixture.Options.Experimental.BeforeSendMetricInternal!.Invoke(metric);

        metric.TryGetAttribute<string>(nameof(Int32), out var value).Should().BeFalse();
        value.Should().BeNull();
    }

    private static SentryMetric<T> CreateCounter<T>(T value) where T : struct
    {
        return new SentryMetric<T>(DateTimeOffset.MinValue, SentryId.Empty, SentryMetricType.Counter, "sentry_tests.sentry_trace_metrics_tests.counter", value);
    }
}

file static class Callback<T> where T : struct
{
    internal static SentryMetric<T>? Nop(SentryMetric<T> metric)
    {
        return metric;
    }
}
