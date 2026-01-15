#nullable enable

namespace Sentry.Tests;

/// <summary>
/// <see href="https://develop.sentry.dev/sdk/telemetry/metrics/"/>
/// </summary>
public partial class SentryTraceMetricsTests : IDisposable
{
    internal sealed class Fixture
    {
        public Fixture()
        {
            DiagnosticLogger = new InMemoryDiagnosticLogger();
            Hub = Substitute.For<IHub>();
            Options = new SentryOptions
            {
                Debug = true,
                DiagnosticLogger = DiagnosticLogger,
            };
            Clock = new MockClock(new DateTimeOffset(2025, 04, 22, 14, 51, 00, 789, TimeSpan.FromHours(2)));
            BatchSize = 2;
            BatchTimeout = Timeout.InfiniteTimeSpan;
            TraceId = SentryId.Create();
            SpanId = Sentry.SpanId.Create();

            Hub.IsEnabled.Returns(true);

            var span = Substitute.For<ISpan>();
            span.TraceId.Returns(TraceId);
            span.SpanId.Returns(SpanId.Value);
            Hub.GetSpan().Returns(span);

            ExpectedAttributes = new Dictionary<string, string>(1)
            {
                { "attribute-key", "attribute-value" },
            };
        }

        public InMemoryDiagnosticLogger DiagnosticLogger { get; }
        public IHub Hub { get; }
        public SentryOptions Options { get; }
        public ISystemClock Clock { get; }
        public int BatchSize { get; set; }
        public TimeSpan BatchTimeout { get; set; }
        public SentryId TraceId { get; private set; }
        public SpanId? SpanId { get; private set; }

        public Dictionary<string, string> ExpectedAttributes { get; }

        public void WithoutActiveSpan()
        {
            Hub.GetSpan().Returns((ISpan?)null);

            var scope = new Scope();
            Hub.SubstituteConfigureScope(scope);
            TraceId = scope.PropagationContext.TraceId;
            SpanId = null;
        }

        public SentryTraceMetrics GetSut() => SentryTraceMetrics.Create(Hub, Options, Clock, BatchSize, BatchTimeout);
    }

    private readonly Fixture _fixture;

    public SentryTraceMetricsTests()
    {
        _fixture = new Fixture();
    }

    public void Dispose()
    {
        _fixture.DiagnosticLogger.Entries.Should().BeEmpty();
    }

    [Fact]
    public void Create_Enabled_NewDefaultInstance()
    {
        Assert.True(_fixture.Options.Experimental.EnableMetrics);

        var instance = _fixture.GetSut();
        var other = _fixture.GetSut();

        instance.Should().BeOfType<DefaultSentryTraceMetrics>();
        instance.Should().NotBeSameAs(other);
    }

    [Fact]
    public void Create_Disabled_CachedDisabledInstance()
    {
        _fixture.Options.Experimental.EnableMetrics = false;

        var instance = _fixture.GetSut();
        var other = _fixture.GetSut();

        instance.Should().BeOfType<DisabledSentryTraceMetrics>();
        instance.Should().BeSameAs(other);
    }

    [Fact]
    public void Emit_WithoutActiveSpan_CapturesEnvelope()
    {
        _fixture.WithoutActiveSpan();
        Assert.True(_fixture.Options.Experimental.EnableMetrics);
        var metrics = _fixture.GetSut();

        Envelope envelope = null!;
        _fixture.Hub.CaptureEnvelope(Arg.Do<Envelope>(arg => envelope = arg));

        metrics.EmitCounter<int>("sentry_tests.sentry_trace_metrics_tests.counter", 1, [new KeyValuePair<string, object>("attribute-key", "attribute-value")]);
        metrics.Flush();

        _fixture.Hub.Received(1).CaptureEnvelope(Arg.Any<Envelope>());
        _fixture.AssertEnvelope<int>(envelope, SentryMetricType.Counter);
    }

    [Fact]
    public void Emit_WithBeforeSendMetric_InvokesCallback()
    {
        var invocations = 0;
        SentryMetric<int> configuredMetric = null!;

        Assert.True(_fixture.Options.Experimental.EnableMetrics);
        _fixture.Options.Experimental.SetBeforeSendMetric<int>((SentryMetric<int> metric) =>
        {
            invocations++;
            configuredMetric = metric;
            return metric;
        });
        var metrics = _fixture.GetSut();

        metrics.EmitCounter<int>("sentry_tests.sentry_trace_metrics_tests.counter", 1, [new KeyValuePair<string, object>("attribute-key", "attribute-value")]);
        metrics.Flush();

        _fixture.Hub.Received(1).CaptureEnvelope(Arg.Any<Envelope>());
        invocations.Should().Be(1);
        _fixture.AssertMetric<int>(configuredMetric, SentryMetricType.Counter);
    }

    [Fact]
    public void Emit_WhenBeforeSendMetricReturnsNull_DoesNotCaptureEnvelope()
    {
        var invocations = 0;

        Assert.True(_fixture.Options.Experimental.EnableMetrics);
        _fixture.Options.Experimental.SetBeforeSendMetric<int>((SentryMetric<int> metric) =>
        {
            invocations++;
            return null;
        });
        var metrics = _fixture.GetSut();

        metrics.EmitCounter<int>("sentry_tests.sentry_trace_metrics_tests.counter", 1, [new KeyValuePair<string, object>("attribute-key", "attribute-value")]);

        _fixture.Hub.Received(0).CaptureEnvelope(Arg.Any<Envelope>());
        invocations.Should().Be(1);
    }

    [Fact]
    public void Emit_InvalidBeforeSendMetric_DoesNotCaptureEnvelope()
    {
        Assert.True(_fixture.Options.Experimental.EnableMetrics);
        _fixture.Options.Experimental.SetBeforeSendMetric<int>(static (SentryMetric<int> metric) => throw new InvalidOperationException());
        var metrics = _fixture.GetSut();

        metrics.EmitCounter<int>("sentry_tests.sentry_trace_metrics_tests.counter", 1);

        _fixture.Hub.Received(0).CaptureEnvelope(Arg.Any<Envelope>());
        var entry = _fixture.DiagnosticLogger.Dequeue();
        entry.Level.Should().Be(SentryLevel.Error);
        entry.Message.Should().Be("The BeforeSendMetric callback threw an exception. The Metric will be dropped.");
        entry.Exception.Should().BeOfType<InvalidOperationException>();
        entry.Args.Should().BeEmpty();
    }

    [Fact]
    public void Flush_AfterEmit_CapturesEnvelope()
    {
        Assert.True(_fixture.Options.Experimental.EnableMetrics);
        var metrics = _fixture.GetSut();

        Envelope envelope = null!;
        _fixture.Hub.CaptureEnvelope(Arg.Do<Envelope>(arg => envelope = arg));

        metrics.Flush();
        _fixture.Hub.Received(0).CaptureEnvelope(Arg.Any<Envelope>());
        envelope.Should().BeNull();

        metrics.EmitCounter<int>("sentry_tests.sentry_trace_metrics_tests.counter", 1, [new KeyValuePair<string, object>("attribute-key", "attribute-value")]);
        _fixture.Hub.Received(0).CaptureEnvelope(Arg.Any<Envelope>());
        envelope.Should().BeNull();

        metrics.Flush();
        _fixture.Hub.Received(1).CaptureEnvelope(Arg.Any<Envelope>());
        _fixture.AssertEnvelope<int>(envelope, SentryMetricType.Counter);
    }

    [Fact]
    public void Dispose_BeforeEmit_DoesNotCaptureEnvelope()
    {
        Assert.True(_fixture.Options.Experimental.EnableMetrics);
        var metrics = _fixture.GetSut();

        var defaultMetrics = metrics.Should().BeOfType<DefaultSentryTraceMetrics>().Which;
        defaultMetrics.Dispose();
        metrics.EmitCounter<int>("sentry_tests.sentry_trace_metrics_tests.counter", 1, [new KeyValuePair<string, object>("attribute-key", "attribute-value")]);

        _fixture.Hub.Received(0).CaptureEnvelope(Arg.Any<Envelope>());
        var entry = _fixture.DiagnosticLogger.Dequeue();
        entry.Level.Should().Be(SentryLevel.Info);
        entry.Message.Should().Be("{0}-Buffer full ... dropping {0}");
        entry.Exception.Should().BeNull();
        entry.Args.Should().BeEquivalentTo([typeof(SentryMetric<int>).Name]);
    }
}

internal static class MetricsAssertionExtensions
{
    public static void AssertEnvelope<T>(this SentryTraceMetricsTests.Fixture fixture, Envelope envelope, SentryMetricType type) where T : struct
    {
        envelope.Header.Should().ContainSingle().Which.Key.Should().Be("sdk");
        var item = envelope.Items.Should().ContainSingle().Which;

        var metric = item.Payload.Should().BeOfType<JsonSerializable>().Which.Source.Should().BeOfType<TraceMetric>().Which;
        AssertMetric<T>(fixture, metric, type);

        Assert.Collection(item.Header,
            element => Assert.Equal(CreateHeader("type", "trace_metric"), element),
            element => Assert.Equal(CreateHeader("item_count", 1), element),
            element => Assert.Equal(CreateHeader("content_type", "application/vnd.sentry.items.trace-metric+json"), element));
    }

    public static void AssertEnvelopeWithoutAttributes<T>(this SentryTraceMetricsTests.Fixture fixture, Envelope envelope, SentryMetricType type) where T : struct
    {
        fixture.ExpectedAttributes.Clear();
        AssertEnvelope<T>(fixture, envelope, type);
    }

    public static void AssertMetric<T>(this SentryTraceMetricsTests.Fixture fixture, TraceMetric metric, SentryMetricType type) where T : struct
    {
        var items = metric.Items;
        items.Length.Should().Be(1);
        var cast = items[0] as SentryMetric<T>;
        Assert.NotNull(cast);
        AssertMetric(fixture, cast, type);
    }

    public static void AssertMetric<T>(this SentryTraceMetricsTests.Fixture fixture, SentryMetric<T> metric, SentryMetricType type) where T : struct
    {
        metric.Timestamp.Should().Be(fixture.Clock.GetUtcNow());
        metric.TraceId.Should().Be(fixture.TraceId);
        metric.Type.Should().Be(type);
        metric.Name.Should().Be("sentry_tests.sentry_trace_metrics_tests.counter");
        metric.Value.Should().Be(1);
        metric.SpanId.Should().Be(fixture.SpanId);
        if (metric.Type is SentryMetricType.Gauge or SentryMetricType.Distribution)
        {
            metric.Unit.Should().Be("measurement_unit");
        }
        else
        {
            metric.Unit.Should().BeNull();
        }

        foreach (var expectedAttribute in fixture.ExpectedAttributes)
        {
            metric.TryGetAttribute(expectedAttribute.Key, out string? value).Should().BeTrue();
            value.Should().Be(expectedAttribute.Value);
        }
    }

    private static KeyValuePair<string, object?> CreateHeader(string name, object? value)
    {
        return new KeyValuePair<string, object?>(name, value);
    }
}
