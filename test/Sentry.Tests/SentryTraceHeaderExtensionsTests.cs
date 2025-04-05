namespace Sentry.Tests;

public class SentryTraceHeaderExtensionsTests
{
    [Theory]
    [InlineData(true, "01")]
    [InlineData(false, "00")]
    public void AsW3CTraceContext_WithSampled_ConvertsToW3CFormat(bool isSampled, string traceFlags)
    {
        // Arrange
        var traceHeader = new SentryTraceHeader(SentryId.Parse("75302ac48a024bde9a3b3734a82e36c8"), SpanId.Parse("1000000000000000"), isSampled);

        // Act
        var result = traceHeader.AsW3CTraceContext();

        // Assert
        result.Should().Be($"00-75302ac48a024bde9a3b3734a82e36c8-1000000000000000-{traceFlags}");
    }

    [Fact]
    public void AsW3CTraceContext_WithoutSampled_ConvertsToW3CFormat()
    {
        // Arrange
        var traceHeader = new SentryTraceHeader(SentryId.Parse("75302ac48a024bde9a3b3734a82e36c8"), SpanId.Parse("1000000000000000"), nuint);

        // Act
        var result = traceHeader.AsW3CTraceContext();

        // Assert
        result.Should().Be("00-75302ac48a024bde9a3b3734a82e36c8-1000000000000000");
    }
}
