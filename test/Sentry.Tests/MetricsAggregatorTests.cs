namespace Sentry.Tests;

public class MetricsAggregatorTests
{
    class Fixture
    {
        public MetricAggregator GetSut()
            => new();
    }

    private readonly Fixture _fixture = new();

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
        var result = MetricAggregator.GetTimeBucketKey(timestamp);

        // Assert
        result.Should().Be(3690); // (1 hour) + (1 minute) plus (30 seconds) = 3690
    }

    [Fact]
    public void GetMetricBucketKey_GeneratesExpectedKey()
    {
        // Arrange
        var type = MetricType.Counter;
        var metricKey = "quibbles";
        var unit = MeasurementUnit.None;
        var tags = new Dictionary<string, string> { ["tag1"] = "value1" };

        // Act
        var result = MetricAggregator.GetMetricBucketKey(type, metricKey, unit, tags);

        // Assert
        result.Should().Be("c_quibbles_none_{\"tag1\":\"value1\"}");
    }

    [Fact]
    public void Increment_NoMetric_CreatesBucketAndMetric()
    {
        // Arrange
        var key = "counter_key";
        var value = 5.0;
        var unit = MeasurementUnit.None;
        var tags = new Dictionary<string, string> { ["tag1"] = "value1" };
        var timestamp = DateTime.UtcNow;

        var sut = new MetricAggregator();

        // Act
        sut.Increment(key, value, unit, tags, timestamp);

        // Assert
        var timeBucket = sut.Buckets[MetricAggregator.GetTimeBucketKey(timestamp)];
        var metric = timeBucket[MetricAggregator.GetMetricBucketKey(MetricType.Counter, key, unit, tags)];

        metric.Value.Should().Be(value);
    }

    [Fact]
    public void Increment_MultipleMetrics_Aggregates()
    {
        // Arrange
        var key = "counter_key";
        var unit = MeasurementUnit.None;
        var tags = new Dictionary<string, string> { ["tag1"] = "value1" };
        var sut = new MetricAggregator();

        // Act
        DateTime firstTime = new(1970, 1, 1, 0, 0, 31, 0, DateTimeKind.Utc);
        sut.Increment(key, 3, unit, tags, firstTime);

        DateTime secondTime = new(1970, 1, 1, 0, 0, 38, 0, DateTimeKind.Utc);
        sut.Increment(key, 5, unit, tags, secondTime);

        DateTime thirdTime = new(1970, 1, 1, 0, 0, 40, 0, DateTimeKind.Utc);
        sut.Increment(key, 13, unit, tags, thirdTime);

        // Assert
        var bucket1 = sut.Buckets[MetricAggregator.GetTimeBucketKey(firstTime)];
        var data1 = bucket1[MetricAggregator.GetMetricBucketKey(MetricType.Counter, key, unit, tags)];
        data1.Value.Should().Be(8); // First two emits are in the same bucket

        var bucket2 = sut.Buckets[MetricAggregator.GetTimeBucketKey(thirdTime)];
        var data2 = bucket2[MetricAggregator.GetMetricBucketKey(MetricType.Counter, key, unit, tags)];
        data2.Value.Should().Be(13); // First two emits are in the same bucket
    }
}
