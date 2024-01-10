using Sentry.Protocol.Metrics;

namespace Sentry.Tests;

public class MetricBucketHelperTests
{
    [Theory]
    [InlineData(30)]
    [InlineData(31)]
    [InlineData(39)]
    public void GetTimeBucketKey_RoundsDownToNearestTenSeconds(int seconds)
    {
        // Arrange
        // Returns the number of seconds that have elapsed since 1970-01-01T00:00:00Z
        var timestamp = new DateTimeOffset(1970, 1, 1, 1, 1, seconds, TimeSpan.Zero);

        // Act
        var result = timestamp.GetTimeBucketKey();

        // Assert
        result.Should().Be(3690); // (1 hour) + (1 minute) plus (30 seconds) = 3690
    }

    [Theory]
    [InlineData(1970, 1, 1, 12, 34, 56, 0)]
    [InlineData(1970, 1, 2, 12, 34, 56, 1)]
    public void GetDayBucketKey_RoundsStartOfDay(int year, int month, int day, int hour, int minute, int second, int expectedDays)
    {
        // Arrange
        var timestamp = new DateTimeOffset(year, month, day, hour, minute, second, TimeSpan.Zero);

        // Act
        var result = timestamp.GetDayBucketKey();

        // Assert
        const int secondsInADay = 60 * 60 * 24;
        result.Should().Be(expectedDays * secondsInADay);
    }
}
