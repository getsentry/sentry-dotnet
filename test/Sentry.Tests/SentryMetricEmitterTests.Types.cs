#nullable enable

namespace Sentry.Tests;

public partial class SentryMetricEmitterTests
{
    [Theory]
    [InlineData(SentryMetricType.Counter)]
    [InlineData(SentryMetricType.Gauge)]
    [InlineData(SentryMetricType.Distribution)]
    public void Emit_Enabled_CapturesEnvelope(SentryMetricType type)
    {
        Assert.True(_fixture.Options.Experimental.EnableMetrics);
        var metrics = _fixture.GetSut();

        Envelope envelope = null!;
        _fixture.Hub.CaptureEnvelope(Arg.Do<Envelope>(arg => envelope = arg));

        metrics.Emit<int>(type, 1, []);
        metrics.Flush();

        _fixture.Hub.Received(1).CaptureEnvelope(Arg.Any<Envelope>());
        _fixture.AssertEnvelopeWithoutAttributes<int>(envelope, type);
    }

    [Theory]
    [InlineData(SentryMetricType.Counter)]
    [InlineData(SentryMetricType.Gauge)]
    [InlineData(SentryMetricType.Distribution)]
    public void Emit_Disabled_DoesNotCaptureEnvelope(SentryMetricType type)
    {
        _fixture.Options.Experimental.EnableMetrics = false;
        var metrics = _fixture.GetSut();

        metrics.Emit<int>(type, 1, []);
        metrics.Flush();

        _fixture.Hub.Received(0).CaptureEnvelope(Arg.Any<Envelope>());
    }

    [Theory]
    [InlineData(SentryMetricType.Counter)]
    [InlineData(SentryMetricType.Gauge)]
    [InlineData(SentryMetricType.Distribution)]
    public void Emit_Attributes_Enabled_CapturesEnvelope(SentryMetricType type)
    {
        Assert.True(_fixture.Options.Experimental.EnableMetrics);
        var metrics = _fixture.GetSut();

        Envelope envelope = null!;
        _fixture.Hub.CaptureEnvelope(Arg.Do<Envelope>(arg => envelope = arg));

        metrics.Emit<int>(type, 1, [new KeyValuePair<string, object>("attribute-key", "attribute-value")]);
        metrics.Flush();

        _fixture.Hub.Received(1).CaptureEnvelope(Arg.Any<Envelope>());
        _fixture.AssertEnvelope<int>(envelope, type);
    }

    [Theory]
    [InlineData(SentryMetricType.Counter)]
    [InlineData(SentryMetricType.Gauge)]
    [InlineData(SentryMetricType.Distribution)]
    public void Emit_Attributes_Disabled_DoesNotCaptureEnvelope(SentryMetricType type)
    {
        _fixture.Options.Experimental.EnableMetrics = false;
        var metrics = _fixture.GetSut();

        metrics.Emit<int>(type, 1, [new KeyValuePair<string, object>("attribute-key", "attribute-value")]);
        metrics.Flush();

        _fixture.Hub.Received(0).CaptureEnvelope(Arg.Any<Envelope>());
    }

    [Theory]
    [InlineData(SentryMetricType.Counter)]
    [InlineData(SentryMetricType.Gauge)]
    [InlineData(SentryMetricType.Distribution)]
    public void Emit_Byte_CapturesEnvelope(SentryMetricType type)
    {
        Assert.True(_fixture.Options.Experimental.EnableMetrics);
        var metrics = _fixture.GetSut();

        Envelope envelope = null!;
        _fixture.Hub.CaptureEnvelope(Arg.Do<Envelope>(arg => envelope = arg));

        metrics.Emit<byte>(type, 1, []);
        metrics.Flush();

        _fixture.Hub.Received(1).CaptureEnvelope(Arg.Any<Envelope>());
        _fixture.AssertEnvelopeWithoutAttributes<byte>(envelope, type);
    }

    [Theory]
    [InlineData(SentryMetricType.Counter)]
    [InlineData(SentryMetricType.Gauge)]
    [InlineData(SentryMetricType.Distribution)]
    public void Emit_Int16_CapturesEnvelope(SentryMetricType type)
    {
        Assert.True(_fixture.Options.Experimental.EnableMetrics);
        var metrics = _fixture.GetSut();

        Envelope envelope = null!;
        _fixture.Hub.CaptureEnvelope(Arg.Do<Envelope>(arg => envelope = arg));

        metrics.Emit<short>(type, 1, []);
        metrics.Flush();

        _fixture.Hub.Received(1).CaptureEnvelope(Arg.Any<Envelope>());
        _fixture.AssertEnvelopeWithoutAttributes<short>(envelope, type);
    }

    [Theory]
    [InlineData(SentryMetricType.Counter)]
    [InlineData(SentryMetricType.Gauge)]
    [InlineData(SentryMetricType.Distribution)]
    public void Emit_Int32_CapturesEnvelope(SentryMetricType type)
    {
        Assert.True(_fixture.Options.Experimental.EnableMetrics);
        var metrics = _fixture.GetSut();

        Envelope envelope = null!;
        _fixture.Hub.CaptureEnvelope(Arg.Do<Envelope>(arg => envelope = arg));

        metrics.Emit<int>(type, 1, []);
        metrics.Flush();

        _fixture.Hub.Received(1).CaptureEnvelope(Arg.Any<Envelope>());
        _fixture.AssertEnvelopeWithoutAttributes<int>(envelope, type);
    }

    [Theory]
    [InlineData(SentryMetricType.Counter)]
    [InlineData(SentryMetricType.Gauge)]
    [InlineData(SentryMetricType.Distribution)]
    public void Emit_Int64_CapturesEnvelope(SentryMetricType type)
    {
        Assert.True(_fixture.Options.Experimental.EnableMetrics);
        var metrics = _fixture.GetSut();

        Envelope envelope = null!;
        _fixture.Hub.CaptureEnvelope(Arg.Do<Envelope>(arg => envelope = arg));

        metrics.Emit<long>(type, 1L, []);
        metrics.Flush();

        _fixture.Hub.Received(1).CaptureEnvelope(Arg.Any<Envelope>());
        _fixture.AssertEnvelopeWithoutAttributes<long>(envelope, type);
    }

    [Theory]
    [InlineData(SentryMetricType.Counter)]
    [InlineData(SentryMetricType.Gauge)]
    [InlineData(SentryMetricType.Distribution)]
    public void Emit_Single_CapturesEnvelope(SentryMetricType type)
    {
        Assert.True(_fixture.Options.Experimental.EnableMetrics);
        var metrics = _fixture.GetSut();

        Envelope envelope = null!;
        _fixture.Hub.CaptureEnvelope(Arg.Do<Envelope>(arg => envelope = arg));

        metrics.Emit<float>(type, 1f, []);
        metrics.Flush();

        _fixture.Hub.Received(1).CaptureEnvelope(Arg.Any<Envelope>());
        _fixture.AssertEnvelopeWithoutAttributes<float>(envelope, type);
    }

    [Theory]
    [InlineData(SentryMetricType.Counter)]
    [InlineData(SentryMetricType.Gauge)]
    [InlineData(SentryMetricType.Distribution)]
    public void Emit_Double_CapturesEnvelope(SentryMetricType type)
    {
        Assert.True(_fixture.Options.Experimental.EnableMetrics);
        var metrics = _fixture.GetSut();

        Envelope envelope = null!;
        _fixture.Hub.CaptureEnvelope(Arg.Do<Envelope>(arg => envelope = arg));

        metrics.Emit<double>(type, 1d, []);
        metrics.Flush();

        _fixture.Hub.Received(1).CaptureEnvelope(Arg.Any<Envelope>());
        _fixture.AssertEnvelopeWithoutAttributes<double>(envelope, type);
    }

    [Theory]
    [InlineData(SentryMetricType.Counter)]
    [InlineData(SentryMetricType.Gauge)]
    [InlineData(SentryMetricType.Distribution)]
    public void Emit_Decimal_DoesNotCaptureEnvelope(SentryMetricType type)
    {
        Assert.True(_fixture.Options.Experimental.EnableMetrics);
        var metrics = _fixture.GetSut();

        metrics.Emit<decimal>(type, 1m, []);
        metrics.Flush();

        _fixture.Hub.Received(0).CaptureEnvelope(Arg.Any<Envelope>());
        var entry = _fixture.DiagnosticLogger.Dequeue();
        entry.Level.Should().Be(SentryLevel.Warning);
        entry.Message.Should().Be("{0} is unsupported type for Sentry Metrics. The only supported types are byte, short, int, long, float, and double.");
        entry.Exception.Should().BeNull();
        entry.Args.Should().BeEquivalentTo([typeof(decimal)]);
    }

#if NET5_0_OR_GREATER
    [Theory]
    [InlineData(SentryMetricType.Counter)]
    [InlineData(SentryMetricType.Gauge)]
    [InlineData(SentryMetricType.Distribution)]
    public void Emit_Half_DoesNotCaptureEnvelope(SentryMetricType type)
    {
        Assert.True(_fixture.Options.Experimental.EnableMetrics);
        var metrics = _fixture.GetSut();

        metrics.Emit<Half>(type, Half.One, []);
        metrics.Flush();

        _fixture.Hub.Received(0).CaptureEnvelope(Arg.Any<Envelope>());
        var entry = _fixture.DiagnosticLogger.Dequeue();
        entry.Level.Should().Be(SentryLevel.Warning);
        entry.Message.Should().Be("{0} is unsupported type for Sentry Metrics. The only supported types are byte, short, int, long, float, and double.");
        entry.Exception.Should().BeNull();
        entry.Args.Should().BeEquivalentTo([typeof(Half)]);
    }
#endif

    [Theory]
    [InlineData(SentryMetricType.Counter)]
    [InlineData(SentryMetricType.Gauge)]
    [InlineData(SentryMetricType.Distribution)]
    public void Emit_Enum_DoesNotCaptureEnvelope(SentryMetricType type)
    {
        Assert.True(_fixture.Options.Experimental.EnableMetrics);
        var metrics = _fixture.GetSut();

        metrics.Emit<StringComparison>(type, (StringComparison)1, []);
        metrics.Flush();

        _fixture.Hub.Received(0).CaptureEnvelope(Arg.Any<Envelope>());
        var entry = _fixture.DiagnosticLogger.Dequeue();
        entry.Level.Should().Be(SentryLevel.Warning);
        entry.Message.Should().Be("{0} is unsupported type for Sentry Metrics. The only supported types are byte, short, int, long, float, and double.");
        entry.Exception.Should().BeNull();
        entry.Args.Should().BeEquivalentTo([typeof(StringComparison)]);
    }

    [Theory]
    [InlineData(SentryMetricType.Counter, nameof(SentryMetricType.Counter), typeof(int))]
    [InlineData(SentryMetricType.Gauge, nameof(SentryMetricType.Gauge), typeof(int))]
    [InlineData(SentryMetricType.Distribution, nameof(SentryMetricType.Distribution), typeof(int))]
    public void Emit_Name_Null_DoesNotCaptureEnvelope(SentryMetricType type, string arg0, Type arg1)
    {
        Assert.True(_fixture.Options.Experimental.EnableMetrics);
        var metrics = _fixture.GetSut();

        metrics.Emit<int>(type, null!, 1);
        metrics.Flush();

        _fixture.Hub.Received(0).CaptureEnvelope(Arg.Any<Envelope>());
        var entry = _fixture.DiagnosticLogger.Dequeue();
        entry.Level.Should().Be(SentryLevel.Warning);
        entry.Message.Should().Be("Name of metrics cannot be null or empty. Metric-Type: {0}; Value-Type: {1}");
        entry.Exception.Should().BeNull();
        entry.Args.Should().BeEquivalentTo<object>([arg0, arg1]);
    }

    [Theory]
    [InlineData(SentryMetricType.Counter, nameof(SentryMetricType.Counter), typeof(int))]
    [InlineData(SentryMetricType.Gauge, nameof(SentryMetricType.Gauge), typeof(int))]
    [InlineData(SentryMetricType.Distribution, nameof(SentryMetricType.Distribution), typeof(int))]
    public void Emit_Name_Empty_DoesNotCaptureEnvelope(SentryMetricType type, string arg0, Type arg1)
    {
        Assert.True(_fixture.Options.Experimental.EnableMetrics);
        var metrics = _fixture.GetSut();

        metrics.Emit<int>(type, "", 1);
        metrics.Flush();

        _fixture.Hub.Received(0).CaptureEnvelope(Arg.Any<Envelope>());
        var entry = _fixture.DiagnosticLogger.Dequeue();
        entry.Level.Should().Be(SentryLevel.Warning);
        entry.Message.Should().Be("Name of metrics cannot be null or empty. Metric-Type: {0}; Value-Type: {1}");
        entry.Exception.Should().BeNull();
        entry.Args.Should().BeEquivalentTo<object>([arg0, arg1]);
    }

    [Fact]
    public void Type_EmitMethods_StringUnitParameterIsObsoleteForForwardCompatibility()
    {
        var type = typeof(SentryMetricEmitter);

        type.Methods()
            .Where(static method => method.IsPublic && method.ReturnType == typeof(void) && method.IsGenericMethod && method.Name.StartsWith("Emit"))
            .Should().NotBeEmpty().And.AllSatisfy(static method =>
            {
                var unitParameter = method.GetParameters().SingleOrDefault(static parameter => parameter.Name == "unit");

                if (unitParameter is null || unitParameter.ParameterType == typeof(MeasurementUnit))
                {
                    method.GetCustomAttribute<ObsoleteAttribute>().Should().BeNull("because Method '{0}' does not take a 'unit' as a 'string'", method);
                }
                else
                {
                    unitParameter.ParameterType.Should().Be(typeof(string));

                    var obsolete = method.GetCustomAttribute<ObsoleteAttribute>();
                    obsolete.Should().NotBeNull("because Method '{0}' does take a 'unit' as a 'string'", method);
                    obsolete.Message.Should().Be(SentryMetricEmitter.ObsoleteStringUnitForwardCompatibility);
                    obsolete.IsError.Should().BeFalse();
                }
            });
    }

    [Theory]
    [InlineData(SentryMetricType.Gauge)]
    [InlineData(SentryMetricType.Distribution)]
    public void Emit_Unit_String_CapturesEnvelope(SentryMetricType type)
    {
        Assert.True(_fixture.Options.Experimental.EnableMetrics);
        var metrics = _fixture.GetSut();

        Envelope envelope = null!;
        _fixture.Hub.CaptureEnvelope(Arg.Do<Envelope>(arg => envelope = arg));

        metrics.Emit<int>(type, 1, "measurement_unit");
        metrics.Flush();

        _fixture.Hub.Received(1).CaptureEnvelope(Arg.Any<Envelope>());
        _fixture.AssertEnvelopeWithoutAttributes<int>(envelope, type);
    }

    [Theory]
    [InlineData(SentryMetricType.Gauge)]
    [InlineData(SentryMetricType.Distribution)]
    public void Emit_Unit_MeasurementUnit_CapturesEnvelope(SentryMetricType type)
    {
        Assert.True(_fixture.Options.Experimental.EnableMetrics);
        var metrics = _fixture.GetSut();

        Envelope envelope = null!;
        _fixture.Hub.CaptureEnvelope(Arg.Do<Envelope>(arg => envelope = arg));

        metrics.Emit<int>(type, 1, MeasurementUnit.Custom("measurement_unit"));
        metrics.Flush();

        _fixture.Hub.Received(1).CaptureEnvelope(Arg.Any<Envelope>());
        _fixture.AssertEnvelopeWithoutAttributes<int>(envelope, type);
    }
}

[Obsolete(SentryMetricEmitter.ObsoleteStringUnitForwardCompatibility)]
file static class SentryMetricEmitterExtensions
{
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

    public static void Emit<T>(this SentryMetricEmitter metrics, SentryMetricType type, string name, T value) where T : struct
    {
        switch (type)
        {
            case SentryMetricType.Counter:
                metrics.EmitCounter<T>(name, value);
                break;
            case SentryMetricType.Gauge:
                metrics.EmitGauge<T>(name, value, "measurement_unit");
                break;
            case SentryMetricType.Distribution:
                metrics.EmitDistribution<T>(name, value, "measurement_unit");
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }

    public static void Emit<T>(this SentryMetricEmitter metrics, SentryMetricType type, T value, string? unit) where T : struct
    {
        switch (type)
        {
            case SentryMetricType.Counter:
                throw new NotSupportedException($"{nameof(SentryMetric<>.Unit)} for {nameof(SentryMetricType.Counter)} is not supported.");
            case SentryMetricType.Gauge:
                metrics.EmitGauge<T>("sentry_tests.sentry_trace_metrics_tests.counter", value, unit);
                break;
            case SentryMetricType.Distribution:
                metrics.EmitDistribution<T>("sentry_tests.sentry_trace_metrics_tests.counter", value, unit);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }

    public static void Emit<T>(this SentryMetricEmitter metrics, SentryMetricType type, T value, MeasurementUnit unit) where T : struct
    {
        switch (type)
        {
            case SentryMetricType.Counter:
                throw new NotSupportedException($"{nameof(SentryMetric<>.Unit)} for {nameof(SentryMetricType.Counter)} is not supported.");
            case SentryMetricType.Gauge:
                metrics.EmitGauge<T>("sentry_tests.sentry_trace_metrics_tests.counter", value, unit);
                break;
            case SentryMetricType.Distribution:
                metrics.EmitDistribution<T>("sentry_tests.sentry_trace_metrics_tests.counter", value, unit);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }
}
