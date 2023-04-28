namespace Sentry.Tests;

public class HttpStatusCodeRangeTests
{
    [Fact]
    public void HttpStatusCodeRange_Includes_Start()
    {
        // Arrange
        var start = 100;
        var end = 200;
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
        var start = 100;
        var end = 200;

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
        var start = 100;
        var end = 200;
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
        var start = 100;
        var end = 200;
        HttpStatusCodeRange sut = (start, end);

        // Act
        var inRange = sut.Contains(201);

        // Assert
        inRange.Should().BeFalse();
    }

    [Fact]
    public void HttpStatusCodeRange_Start_After_End_Throws()
    {
        // Arrange
        var start = 200;
        var end = 100;

        // Act & Assert
        Assert.ThrowsAny<ArgumentOutOfRangeException>(() => new HttpStatusCodeRange(start, end));
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
