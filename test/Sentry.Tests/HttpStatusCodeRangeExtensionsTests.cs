namespace Sentry.Tests;

public class HttpStatusCodeRangeExtensionsTests
{
    [Fact]
    public void Contains_EmptyList_ReturnsFalse()
    {
        // Arrange
        var ranges = new List<HttpStatusCodeRange>();

        // Act
        var result = ranges.ContainsStatusCode(404);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData(400)]
    [InlineData(450)]
    [InlineData(499)]
    public void Contains_SingleRange_InRange_ReturnsTrue(int statusCode)
    {
        // Arrange
        var ranges = new List<HttpStatusCodeRange> { (400, 499) };

        // Act
        var result = ranges.ContainsStatusCode(statusCode);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(200)]
    [InlineData(399)]
    [InlineData(500)]
    public void Contains_SingleRange_OutOfRange_ReturnsFalse(int statusCode)
    {
        // Arrange
        var ranges = new List<HttpStatusCodeRange> { (400, 499) };

        // Act
        var result = ranges.ContainsStatusCode(statusCode);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData(400)] // In first range
    [InlineData(404)] // In first range
    [InlineData(500)] // In second range
    [InlineData(503)] // In second range
    public void Contains_MultipleRanges_InAnyRange_ReturnsTrue(int statusCode)
    {
        // Arrange
        var ranges = new List<HttpStatusCodeRange> { (400, 404), (500, 503) };

        // Act
        var result = ranges.ContainsStatusCode(statusCode);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(200)] // Below ranges
    [InlineData(405)] // Between ranges
    [InlineData(499)] // Between ranges
    [InlineData(504)] // Above ranges
    public void Contains_MultipleRanges_NotInAnyRange_ReturnsFalse(int statusCode)
    {
        // Arrange
        var ranges = new List<HttpStatusCodeRange> { (400, 404), (500, 503) };

        // Act
        var result = ranges.ContainsStatusCode(statusCode);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData(400)] // In first range only
    [InlineData(425)] // In overlap
    [InlineData(450)] // In overlap
    [InlineData(475)] // In second range only
    public void Contains_OverlappingRanges_InUnion_ReturnsTrue(int statusCode)
    {
        // Arrange
        var ranges = new List<HttpStatusCodeRange> { (400, 450), (425, 475) };

        // Act
        var result = ranges.ContainsStatusCode(statusCode);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(200)]
    [InlineData(399)]
    [InlineData(476)]
    [InlineData(500)]
    public void Contains_OverlappingRanges_OutsideUnion_ReturnsFalse(int statusCode)
    {
        // Arrange
        var ranges = new List<HttpStatusCodeRange> { (400, 450), (425, 475) };

        // Act
        var result = ranges.ContainsStatusCode(statusCode);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Contains_SingleValueRange_ExactMatch_ReturnsTrue()
    {
        // Arrange
        var ranges = new List<HttpStatusCodeRange> { 404 };

        // Act
        var result = ranges.ContainsStatusCode(404);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(403)]
    [InlineData(405)]
    public void Contains_SingleValueRange_NoMatch_ReturnsFalse(int statusCode)
    {
        // Arrange
        var ranges = new List<HttpStatusCodeRange> { 404 };

        // Act
        var result = ranges.ContainsStatusCode(statusCode);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Contains_HttpStatusCodeEnum_CallsIntOverload()
    {
        // Arrange
        var ranges = new List<HttpStatusCodeRange> { (400, 499) };

        // Act
        var result = ranges.ContainsStatusCode(HttpStatusCode.NotFound); // 404

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(400)] // Start of range
    [InlineData(499)] // End of range
    public void Contains_RangeBoundaries_ReturnsTrue(int statusCode)
    {
        // Arrange
        var ranges = new List<HttpStatusCodeRange> { (400, 499) };

        // Act
        var result = ranges.ContainsStatusCode(statusCode);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(399)] // Just before start
    [InlineData(500)] // Just after end
    public void Contains_JustOutsideBoundaries_ReturnsFalse(int statusCode)
    {
        // Arrange
        var ranges = new List<HttpStatusCodeRange> { (400, 499) };

        // Act
        var result = ranges.ContainsStatusCode(statusCode);

        // Assert
        result.Should().BeFalse();
    }
}
