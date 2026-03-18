namespace Sentry.Tests.Protocol;

public class W3CTraceparentHeaderTests
{
    [Fact]
    public void ToString_WithSampledTrue_Works()
    {
        // Arrange
        var traceId = SentryId.Parse("75302ac48a024bde9a3b3734a82e36c8");
        var spanId = SpanId.Parse("1000000000000000");
        var header = new W3CTraceparentHeader(traceId, spanId, true);

        // Act
        var result = header.ToString();

        // Assert
        result.Should().Be("00-75302ac48a024bde9a3b3734a82e36c8-1000000000000000-01");
    }

    [Fact]
    public void ToString_WithSampledFalse_Works()
    {
        // Arrange
        var traceId = SentryId.Parse("75302ac48a024bde9a3b3734a82e36c8");
        var spanId = SpanId.Parse("1000000000000000");
        var header = new W3CTraceparentHeader(traceId, spanId, false);

        // Act
        var result = header.ToString();

        // Assert
        result.Should().Be("00-75302ac48a024bde9a3b3734a82e36c8-1000000000000000-00");
    }

    [Fact]
    public void ToString_WithoutSampled_DefaultsToFalse()
    {
        // Arrange
        var traceId = SentryId.Parse("75302ac48a024bde9a3b3734a82e36c8");
        var spanId = SpanId.Parse("1000000000000000");
        var header = new W3CTraceparentHeader(traceId, spanId, null);

        // Act
        var result = header.ToString();

        // Assert
        result.Should().Be("00-75302ac48a024bde9a3b3734a82e36c8-1000000000000000-00");
    }

    [Fact]
    public void Constructor_StoresProperties()
    {
        // Arrange
        var traceId = SentryId.Parse("75302ac48a024bde9a3b3734a82e36c8");
        var spanId = SpanId.Parse("1000000000000000");
        var isSampled = true;

        // Act
        var header = new W3CTraceparentHeader(traceId, spanId, isSampled);

        // Assert
        header.TraceId.Should().Be(traceId);
        header.SpanId.Should().Be(spanId);
        header.IsSampled.Should().Be(isSampled);
    }

    [Fact]
    public void HttpHeaderName_IsCorrect()
    {
        // Act & Assert
        W3CTraceparentHeader.HttpHeaderName.Should().Be("traceparent");
    }
}
