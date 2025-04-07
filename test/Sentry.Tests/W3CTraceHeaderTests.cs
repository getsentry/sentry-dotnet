namespace Sentry.Tests;

public class W3CTraceHeaderTests
{
    [Theory]
    [InlineData(true, "01")]
    [InlineData(false, "00")]
    [InlineData(null, "00")]
    public void ToString_ConvertsToW3CFormat(bool? isSampled, string traceFlags)
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
    public void Parse_ValidW3CHeader_ReturnsW3CTraceHeader()
    {
        // Arrange
        var header = "00-4bc7d217a6721c0e60e85e46d25fb3e5-f51f11f284da5299-01";

        // Act
        var result = W3CTraceHeader.Parse(header);

        // Assert
        result.Should().NotBeNull();
        result.SentryTraceHeader.TraceId.ToString().Should().Be("4bc7d217a6721c0e60e85e46d25fb3e5");
        result.SentryTraceHeader.SpanId.ToString().Should().Be("f51f11f284da5299");
        result.SentryTraceHeader.IsSampled.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\n")]
    [InlineData(null)]
    public void Parse_Returns_Null_WhenHeaderIsNullOrEmpty(string header)
    {
        // Act
        var result = W3CTraceHeader.Parse(header);

        // Assert
        result.Should().BeNull();
    }

    [Theory]
    [InlineData("00-4bc7d217a6721c0e60e85e46d25fb3e5-1000000000000000")]
    [InlineData("01-f5cb855e16344ddd1538d10f82f6a018-a7018579d434fee4")]
    public void Parse_InvalidW3CHeader_ThrowsFormatException(string header)
    {
        // Act
        Action act = () => W3CTraceHeader.Parse(header);

        // Assert
        act.Should().Throw<FormatException>();
    }

    [Fact]
    public void Null_Source_Throws_ArgumentNullException()
    {
        // Arrange &Act
        Action act = static () => new W3CTraceHeader(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("source").WithMessage("*source*");
    }
}
