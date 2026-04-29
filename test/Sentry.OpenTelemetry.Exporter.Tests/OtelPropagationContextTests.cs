using Sentry.OpenTelemetry.Tests;

namespace Sentry.OpenTelemetry.Exporter.Tests;

public class OtelPropagationContextTests : ActivitySourceTests
{
    private class Fixture
    {
        public SentryId ActiveReplayId { get; } = SentryId.Create();
        public IReplaySession ActiveReplaySession { get; }
        public SentryOptions SentryOptions { get; }

        public Fixture()
        {
            ActiveReplaySession = Substitute.For<IReplaySession>();
            ActiveReplaySession.ActiveReplayId.Returns(ActiveReplayId);

            SentryOptions = new SentryOptions { Dsn = "https://examplePublicKey@o0.ingest.sentry.io/123456" };
        }
    }

    private readonly Fixture _fixture = new();

    [Fact]
    public void TraceId_NoActivityCurrent_ReturnsNull()
    {
        // Arrange
        var sut = new OtelPropagationContext();
        Activity.Current = null;

        // Act
        var traceId = sut.TraceId;

        // Assert
        traceId.Should().BeNull();
    }

    [Fact]
    public void TraceId_WithActivityCurrent_ReturnsSentryIdFromActivityTraceId()
    {
        // Arrange
        using var activity = Tracer.StartActivity();
        var sut = new OtelPropagationContext();

        // Act
        var traceId = sut.TraceId;

        // Assert
        traceId.Should().NotBe(default(SentryId));
        traceId.Should().Be(activity.TraceId.AsSentryId());
    }

    [Fact]
    public void SpanId_NoActivityCurrent_ReturnsNull()
    {
        // Arrange
        var sut = new OtelPropagationContext();
        Activity.Current = null;

        // Act
        var spanId = sut.SpanId;

        // Assert
        spanId.Should().BeNull();
    }

    [Fact]
    public void SpanId_WithActivityCurrent_ReturnsSpanIdFromActivitySpanId()
    {
        // Arrange
        using var activity = Tracer.StartActivity();
        var sut = new OtelPropagationContext();

        // Act
        var spanId = sut.SpanId;

        // Assert
        spanId.Should().NotBe(default(SpanId));
        spanId.Should().Be(activity.SpanId.AsSentrySpanId());
    }

    [Fact]
    public void ParentSpanId_NoActivityCurrent_ReturnsNull()
    {
        // Arrange
        var sut = new OtelPropagationContext();
        Activity.Current = null;

        // Act
        var parentSpanId = sut.ParentSpanId;

        // Assert
        parentSpanId.Should().BeNull();
    }

    [Fact]
    public void ParentSpanId_WithRootActivityCurrent_ReturnsNull()
    {
        // Arrange - root activity has no parent, so ParentSpanId is default(ActivitySpanId)
        using var rootActivity = new Activity("root").Start();
        var sut = new OtelPropagationContext();

        // Act
        var parentSpanId = sut.ParentSpanId;

        // Assert
        parentSpanId.Should().BeNull();
    }

    [Fact]
    public void ParentSpanId_WithActivityCurrent_ReturnsParentSpanIdFromActivity()
    {
        // Arrange
        using var parentActivity = new Activity("parent").Start();
        using var childActivity = new Activity("child").Start();
        var sut = new OtelPropagationContext();

        // Act
        var parentSpanId = sut.ParentSpanId;

        // Assert
        parentSpanId.Should().NotBeNull();
        parentSpanId.Should().Be(parentActivity.SpanId.AsSentrySpanId());
    }

    [Fact]
    public void DynamicSamplingContext_ByDefault_IsNull()
    {
        // Arrange & Act
        var sut = new OtelPropagationContext();

        // Assert
        sut.DynamicSamplingContext.Should().BeNull();
    }

    [Fact]
    public void GetDynamicSamplingContext_DynamicSamplingContextIsNull_CreatesDynamicSamplingContext()
    {
        // Arrange
        using var activity = Tracer.StartActivity();
        var sut = new OtelPropagationContext();
        sut.DynamicSamplingContext.Should().BeNull();

        // Act
        var result = sut.GetDynamicSamplingContext(_fixture.SentryOptions, _fixture.ActiveReplaySession);

        // Assert
        result.Should().NotBeNull();
        sut.DynamicSamplingContext.Should().NotBeNull();
        sut.DynamicSamplingContext.Should().BeSameAs(result);
    }

    [Fact]
    public void GetDynamicSamplingContext_DynamicSamplingContextIsNotNull_ReturnsSameDynamicSamplingContext()
    {
        // Arrange
        using var activity = Tracer.StartActivity();
        var sut = new OtelPropagationContext();
        var firstResult = sut.GetDynamicSamplingContext(_fixture.SentryOptions, _fixture.ActiveReplaySession);

        // Act
        var secondResult = sut.GetDynamicSamplingContext(_fixture.SentryOptions, _fixture.ActiveReplaySession);

        // Assert
        firstResult.Should().BeSameAs(secondResult);
        sut.DynamicSamplingContext.Should().BeSameAs(firstResult);
    }

    [Fact]
    public void GetDynamicSamplingContext_WithActiveReplaySession_IncludesReplayIdInDynamicSamplingContext()
    {
        // Arrange
        using var activity = Tracer.StartActivity();
        var sut = new OtelPropagationContext();

        // Act
        var result = sut.GetDynamicSamplingContext(_fixture.SentryOptions, _fixture.ActiveReplaySession);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().Contain(kvp => kvp.Key == "replay_id" && kvp.Value == _fixture.ActiveReplayId.ToString());
    }

    [Fact]
    public void SampleRate_NoActivity_ReturnsNull()
    {
        Activity.Current = null;
        var sut = new OtelPropagationContext();

        sut.SampleRate.Should().BeNull();
    }

    [Fact]
    public void SampleRate_ActivityWithNoTraceState_ReturnsNull()
    {
        using var activity = new Activity("test").Start();
        var sut = new OtelPropagationContext();

        sut.SampleRate.Should().BeNull();
    }

    [Fact]
    public void SampleRate_ActivityWithOtEntryButNoThKey_ReturnsNull()
    {
        using var activity = new Activity("test").Start();
        activity.TraceStateString = "ot=rv:a0000000000000";
        var sut = new OtelPropagationContext();

        sut.SampleRate.Should().BeNull();
    }

    [Theory]
    [InlineData("8", 0.5)]           // "8" -> 0x80000000000000 / 2^56 = 0.5
    [InlineData("4", 0.25)]          // "4" -> 0x40000000000000 / 2^56 = 0.25
    [InlineData("0", 0.0)]           // threshold 0 = never sample
    [InlineData("ffffffffffffff", 1.0 - 1.0 / (1UL << 56))]  // max 56-bit value
    public void SampleRate_ActivityWithThValue_ReturnsParsedRate(string th, double expected)
    {
        using var activity = new Activity("test").Start();
        activity.TraceStateString = $"ot=th:{th}";
        var sut = new OtelPropagationContext();

        sut.SampleRate.Should().BeApproximately(expected, 1e-15);
    }

    [Fact]
    public void SampleRate_ActivityWithMultipleVendors_ParsesOtEntry()
    {
        using var activity = new Activity("test").Start();
        activity.TraceStateString = "other=value,ot=th:8,another=x";
        var sut = new OtelPropagationContext();

        sut.SampleRate.Should().BeApproximately(0.5, 1e-15);
    }

    [Fact]
    public void SampleRand_NoActivity_ReturnsNull()
    {
        Activity.Current = null;
        var sut = new OtelPropagationContext();

        sut.SampleRand.Should().BeNull();
    }

    [Fact]
    public void SampleRand_ActivityWithNoTraceState_ReturnsNull()
    {
        using var activity = new Activity("test").Start();
        var sut = new OtelPropagationContext();

        sut.SampleRand.Should().BeNull();
    }

    [Fact]
    public void SampleRand_ActivityWithOtEntryButNoRvKey_ReturnsNull()
    {
        using var activity = new Activity("test").Start();
        activity.TraceStateString = "ot=th:8";
        var sut = new OtelPropagationContext();

        sut.SampleRand.Should().BeNull();
    }

    [Fact]
    public void SampleRand_ActivityWithRvValue_ReturnsParsedValue()
    {
        // "a" -> 0xa0000000000000 / 2^56 = 0.625
        using var activity = new Activity("test").Start();
        activity.TraceStateString = "ot=rv:a";
        var sut = new OtelPropagationContext();

        sut.SampleRand.Should().BeApproximately(0.625, 1e-15);
    }

    [Fact]
    public void SampleRand_ActivityWithBothThAndRv_ReturnsRvValue()
    {
        // "4" -> 0x40000000000000 / 2^56 = 0.25
        using var activity = new Activity("test").Start();
        activity.TraceStateString = "ot=th:8;rv:4";
        var sut = new OtelPropagationContext();

        sut.SampleRand.Should().BeApproximately(0.25, 1e-15);
    }
}
