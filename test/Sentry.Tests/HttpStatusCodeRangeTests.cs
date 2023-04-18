namespace Sentry.Tests;

public class HttpStatusCodeRangeTests
{
    [Fact]
    public void HttpStatusCodeRange_Includes_Start()
    {
        // Arrange
        int start = 100;
        int end = 200;
        HttpStatusCodeRange sut = (start, end);

        // Act
        var inRange = sut.Contains(100);

        // Assert
        inRange.Should().BeTrue();
    }

    [Fact]
    public void HttpStatusCodeRange_Includes_End()
    {
        // Arrange
        int start = 100;
        int end = 200;

        HttpStatusCodeRange sut = (start, end);

        // Act
        var inRange = sut.Contains(200);

        // Assert
        inRange.Should().BeTrue();
    }

    [Fact]
    public void HttpStatusCodeRange_Excludes_BelowStart()
    {
        // Arrange
        int start = 100;
        int end = 200;
        HttpStatusCodeRange sut = (start, end);

        // Act
        var inRange = sut.Contains(99);

        // Assert
        inRange.Should().BeFalse();
    }

    [Fact]
    public void HttpStatusCodeRange_Excludes_AboveEnd()
    {
        // Arrange
        int start = 100;
        int end = 200;
        HttpStatusCodeRange sut = (start, end);

        // Act
        var inRange = sut.Contains(201);

        // Assert
        inRange.Should().BeFalse();
    }

    [Fact]
    public void HttpStatusCodeRange_Can_Be_Inverted()
    {
        // Arrange
        int start = 100;
        int end = 200;
        HttpStatusCodeRange sut = (end, start);

        // Act
        var inRange = sut.Contains(101);

        // Assert
        inRange.Should().BeTrue();
    }

    [Fact]
    public void HttpStatusCodeRange_Matches_SingleStatusCode()
    {
        // Arrange
        HttpStatusCodeRange sut = HttpStatusCode.InternalServerError;

        // Act
        var inRange = sut.Contains(HttpStatusCode.InternalServerError);

        // Assert
        inRange.Should().BeTrue();
    }
}
