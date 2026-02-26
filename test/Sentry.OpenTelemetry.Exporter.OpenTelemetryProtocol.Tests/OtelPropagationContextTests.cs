using Sentry.OpenTelemetry.Tests;

namespace Sentry.OpenTelemetry.Exporter.OpenTelemetryProtocol.Tests;

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
    public void TraceId_NoActivityCurrent_ReturnsDefault()
    {
        // Arrange
        var sut = new OtelPropagationContext();
        Activity.Current = null;

        // Act
        var traceId = sut.TraceId;

        // Assert
        traceId.Should().Be(default(SentryId));
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
    public void SpanId_NoActivityCurrent_ReturnsDefault()
    {
        // Arrange
        var sut = new OtelPropagationContext();
        Activity.Current = null;

        // Act
        var spanId = sut.SpanId;

        // Assert
        spanId.Should().Be(default(SpanId));
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
    public void GetOrCreateDynamicSamplingContext_DynamicSamplingContextIsNull_CreatesDynamicSamplingContext()
    {
        // Arrange
        using var activity = Tracer.StartActivity();
        var sut = new OtelPropagationContext();
        sut.DynamicSamplingContext.Should().BeNull();

        // Act
        var result = sut.GetOrCreateDynamicSamplingContext(_fixture.SentryOptions, _fixture.ActiveReplaySession);

        // Assert
        result.Should().NotBeNull();
        sut.DynamicSamplingContext.Should().NotBeNull();
        sut.DynamicSamplingContext.Should().BeSameAs(result);
    }

    [Fact]
    public void GetOrCreateDynamicSamplingContext_DynamicSamplingContextIsNotNull_ReturnsSameDynamicSamplingContext()
    {
        // Arrange
        using var activity = Tracer.StartActivity();
        var sut = new OtelPropagationContext();
        var firstResult = sut.GetOrCreateDynamicSamplingContext(_fixture.SentryOptions, _fixture.ActiveReplaySession);

        // Act
        var secondResult = sut.GetOrCreateDynamicSamplingContext(_fixture.SentryOptions, _fixture.ActiveReplaySession);

        // Assert
        firstResult.Should().BeSameAs(secondResult);
        sut.DynamicSamplingContext.Should().BeSameAs(firstResult);
    }

    [Fact]
    public void GetOrCreateDynamicSamplingContext_WithActiveReplaySession_IncludesReplayIdInDynamicSamplingContext()
    {
        // Arrange
        using var activity = Tracer.StartActivity();
        var sut = new OtelPropagationContext();

        // Act
        var result = sut.GetOrCreateDynamicSamplingContext(_fixture.SentryOptions, _fixture.ActiveReplaySession);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().Contain(kvp => kvp.Key == "replay_id" && kvp.Value == _fixture.ActiveReplayId.ToString());
    }
}
