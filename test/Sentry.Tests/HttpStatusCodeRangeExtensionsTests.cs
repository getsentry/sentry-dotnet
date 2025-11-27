namespace Sentry.Tests;

public class HttpStatusCodeRangeExtensionsTests
{
    [Fact]
    public void ContainsStatusCode_EmptyList_ReturnsFalse()
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
    public void ContainsStatusCode_SingleRangeInRange_ReturnsTrue(int statusCode)
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
    public void ContainsStatusCode_SingleRangeOutOfRange_ReturnsFalse(int statusCode)
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
    public void ContainsStatusCode_MultipleRangesInAnyRange_ReturnsTrue(int statusCode)
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
    public void ContainsStatusCode_MultipleRangesNotInAnyRange_ReturnsFalse(int statusCode)
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
    public void ContainsStatusCode_OverlappingRangesInUnion_ReturnsTrue(int statusCode)
    {
        // Arrange
        var ranges = new List<HttpStatusCodeRange> { (400, 450), (425, 475) };

        // Act
        var result = ranges.ContainsStatusCode(statusCode);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(200)] // Below first range
    [InlineData(399)] // Below first range
    [InlineData(476)] // Above second range
    [InlineData(500)] // Above second range
    public void ContainsStatusCode_OverlappingRangesOutsideUnion_ReturnsFalse(int statusCode)
    {
        // Arrange
        var ranges = new List<HttpStatusCodeRange> { (400, 450), (425, 475) };

        // Act
        var result = ranges.ContainsStatusCode(statusCode);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ContainsStatusCode_SingleValueRangeExactMatch_ReturnsTrue()
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
    public void ContainsStatusCode_SingleValueRangeNoMatch_ReturnsFalse(int statusCode)
    {
        // Arrange
        var ranges = new List<HttpStatusCodeRange> { 404 };

        // Act
        var result = ranges.ContainsStatusCode(statusCode);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ContainsStatusCode_HttpStatusCodeEnumInRange_ReturnsTrue()
    {
        // Arrange
        var ranges = new List<HttpStatusCodeRange> { (400, 499) };

        // Act
        var result = ranges.ContainsStatusCode(HttpStatusCode.NotFound); // 404

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ContainsStatusCode_HttpStatusCodeEnumOutOfRange_ReturnsFalse()
    {
        // Arrange
        var ranges = new List<HttpStatusCodeRange> { (400, 499) };

        // Act
        var result = ranges.ContainsStatusCode(HttpStatusCode.OK); // 200

        // Assert
        result.Should().BeFalse();
    }
}
