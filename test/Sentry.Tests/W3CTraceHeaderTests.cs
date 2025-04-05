namespace Sentry.Tests;

public class W3CTraceHeaderTests
{
    [Theory]
    [InlineData(true, "01")]
    [InlineData(false, "00")]
    public void ToString_WithSampled_ConvertsToW3CFormat(bool isSampled, string traceFlags)
    {
        // Arrange
        var source = new SentryTraceHeader(SentryId.Parse("75302ac48a024bde9a3b3734a82e36c8"), SpanId.Parse("1000000000000000"), isSampled);
        var traceHeader = new W3CTraceHeader(source);

        // Act
        var result = traceHeader.ToString();

        // Assert
        result.Should().Be($"00-75302ac48a024bde9a3b3734a82e36c8-1000000000000000-{traceFlags}");
    }

    [Fact]
    public void ToString_WithoutSampled_ConvertsToW3CFormat()
    {
        // Arrange
        var source = new SentryTraceHeader(SentryId.Parse("75302ac48a024bde9a3b3734a82e36c8"), SpanId.Parse("1000000000000000"), null);
        var traceHeader = new W3CTraceHeader(source);

        // Act
        var result = traceHeader.ToString();

        // Assert
        result.Should().Be("00-75302ac48a024bde9a3b3734a82e36c8-1000000000000000");
    }
}
