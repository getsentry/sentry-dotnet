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
        // var timestamp = new DateTime(2023, 1, 15, 17, 42, 31, DateTimeKind.Utc);
        var timestamp = new DateTime(1970, 1, 1, 1, 1, seconds, DateTimeKind.Utc);

        // Act
        var result = timestamp.GetTimeBucketKey();

        // Assert
        result.Should().Be(3690); // (1 hour) + (1 minute) plus (30 seconds) = 3690
    }
}
