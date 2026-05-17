namespace Sentry.OpenTelemetry.Exporter.Tests;

public class ActivityExtensionsTests
{
    [Fact]
    public void AsSentrySpanId_WithLowBitValue_ConvertsCorrectly()
    {
        // Arrange - high bit clear, value fits in positive long range
        var activitySpanId = ActivitySpanId.CreateFromString("1a2b3c4d5e6f7a8b".AsSpan());

        // Act
        var sentrySpanId = activitySpanId.AsSentrySpanId();

        // Assert
        sentrySpanId.ToString().Should().Be("1a2b3c4d5e6f7a8b");
    }

    [Fact]
    public void AsSentrySpanId_WithHighBitValue_ConvertsCorrectly()
    {
        // Arrange - high bit set, would fail if long.TryParse didn't support two's complement
        var activitySpanId = ActivitySpanId.CreateFromString("a1b2c3d4e5f6a7b8".AsSpan());

        // Act
        var sentrySpanId = activitySpanId.AsSentrySpanId();

        // Assert
        sentrySpanId.ToString().Should().Be("a1b2c3d4e5f6a7b8");
    }

    [Fact]
    public void AsSentrySpanId_ThenAsActivitySpanId_RoundTrips()
    {
        // Arrange
        var original = ActivitySpanId.CreateRandom();

        // Act
        var roundTripped = original.AsSentrySpanId().AsActivitySpanId();

        // Assert
        roundTripped.Should().Be(original);
    }

    [Fact]
    public void AsActivitySpanId_ThenAsSentrySpanId_RoundTrips()
    {
        // Arrange
        var original = SpanId.Create();

        // Act
        var roundTripped = original.AsActivitySpanId().AsSentrySpanId();

        // Assert
        roundTripped.Should().Be(original);
    }

    [Fact]
    public void AsActivitySpanId_WithHighBitValue_ConvertsCorrectly()
    {
        // Arrange
        var sentrySpanId = SpanId.Parse("a1b2c3d4e5f6a7b8");

        // Act
        var activitySpanId = sentrySpanId.AsActivitySpanId();

        // Assert
        activitySpanId.ToHexString().Should().Be("a1b2c3d4e5f6a7b8");
    }

    [Fact]
    public void AsSentryId_WithActivityTraceId_ConvertsCorrectly()
    {
        // Arrange
        var activityTraceId = ActivityTraceId.CreateFromString("5bd5f6d346b442dd9177dce9302fd737".AsSpan());

        // Act
        var sentryId = activityTraceId.AsSentryId();

        // Assert
        sentryId.ToString().Should().Be("5bd5f6d346b442dd9177dce9302fd737");
    }

    [Fact]
    public void AsSentryId_ThenAsActivityTraceId_RoundTrips()
    {
        // Arrange
        var original = ActivityTraceId.CreateRandom();

        // Act
        var roundTripped = original.AsSentryId().AsActivityTraceId();

        // Assert
        roundTripped.Should().Be(original);
    }

    [Fact]
    public void AsActivityTraceId_ThenAsSentryId_RoundTrips()
    {
        // Arrange
        var original = SentryId.Create();

        // Act
        var roundTripped = original.AsActivityTraceId().AsSentryId();

        // Assert
        roundTripped.Should().Be(original);
    }

    [Fact]
    public void AsActivityTraceId_WithFixedValue_ConvertsCorrectly()
    {
        // Arrange
        var sentryId = SentryId.Parse("5bd5f6d346b442dd9177dce9302fd737");

        // Act
        var activityTraceId = sentryId.AsActivityTraceId();

        // Assert
        activityTraceId.ToHexString().Should().Be("5bd5f6d346b442dd9177dce9302fd737");
    }

    [Fact]
    public void SentryIdTryFormat_OutputIsAcceptedByActivityTraceId()
    {
        // Arrange
        var sentryId = SentryId.Parse("5bd5f6d346b442dd9177dce9302fd737");
        Span<char> buffer = stackalloc char[32];

        // Act
        var formatted = sentryId.TryFormat(buffer);
        var activityTraceId = ActivityTraceId.CreateFromString(buffer);

        // Assert
        formatted.Should().BeTrue();
        buffer.ToString().Should().Be("5bd5f6d346b442dd9177dce9302fd737");
        activityTraceId.ToHexString().Should().Be("5bd5f6d346b442dd9177dce9302fd737");
    }
}
