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

    [Theory]
    [InlineData("00-4bc7d217a6721c0e60e85e46d25fb3e5-f51f11f284da5299-01", "4bc7d217a6721c0e60e85e46d25fb3e5", "f51f11f284da5299", true)]
    [InlineData("00-3d19f80b6f7da306d7b5652745ec6173-703b42311109c14e-09", "3d19f80b6f7da306d7b5652745ec6173", "703b42311109c14e", true)]
    [InlineData("00-992d690c7a3691eb0f409a3ba6ecc0cc-b4f1f8cbcc61a0e5-00", "992d690c7a3691eb0f409a3ba6ecc0cc", "b4f1f8cbcc61a0e5", false)]
    [InlineData("00-19938c125f92c552c2e2711393725319-2ce52eea8ffe1335-xz", "19938c125f92c552c2e2711393725319", "2ce52eea8ffe1335", null)] // Invalid trace flags should not cause an exception
    [InlineData("00-fba65b9f95900925670373fc2943339e-c5113ff625da6c9a-420", "fba65b9f95900925670373fc2943339e", "c5113ff625da6c9a", null)] // Invalid trace flags should not cause an exception
    public void Parse_ValidW3CHeader_ReturnsW3CTraceHeader(string header, string expectedTraceId, string expectedSpanId, bool? expectedIsSampled)
    {
        // Act
        var result = W3CTraceHeader.Parse(header);

        // Assert
        result.Should().NotBeNull();
        result.SentryTraceHeader.TraceId.ToString().Should().Be(expectedTraceId);
        result.SentryTraceHeader.SpanId.ToString().Should().Be(expectedSpanId);
        result.SentryTraceHeader.IsSampled.Should().Be(expectedIsSampled);
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
    [InlineData("01-7f97dee64921546b7c238cb8a0c1209d-a82702bf47683069-00")]
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
