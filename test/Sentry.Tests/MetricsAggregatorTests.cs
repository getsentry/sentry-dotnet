using Sentry.Protocol.Metrics;

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
        var type = MetricAggregator.MetricType.Counter;
        var metricKey = "quibbles";
        var unit = MeasurementUnit.None;
        var tags = new Dictionary<string, string> { ["tag1"] = "value1" };

        // Act
        var result = MetricAggregator.GetMetricBucketKey(type, metricKey, unit, tags);

        // Assert
        result.Should().Be("c_quibbles_none_{\"tag1\":\"value1\"}");
    }

    [Fact]
    public void Increment_AggregatesMetrics()
    {
        // Arrange
        var metricType = MetricAggregator.MetricType.Counter;
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
        var data1 = (CounterMetric)bucket1[MetricAggregator.GetMetricBucketKey(metricType, key, unit, tags)];
        data1.Value.Should().Be(8); // First two emits are in the same bucket

        var bucket2 = sut.Buckets[MetricAggregator.GetTimeBucketKey(thirdTime)];
        var data2 = (CounterMetric)bucket2[MetricAggregator.GetMetricBucketKey(metricType, key, unit, tags)];
        data2.Value.Should().Be(13); // First two emits are in the same bucket
    }

    [Fact]
    public void Gauge_AggregatesMetrics()
    {
        // Arrange
        var metricType = MetricAggregator.MetricType.Gauge;
        var key = "gauge_key";
        var unit = MeasurementUnit.None;
        var tags = new Dictionary<string, string> { ["tag1"] = "value1" };
        var sut = new MetricAggregator();

        // Act
        DateTime time1 = new(1970, 1, 1, 0, 0, 31, 0, DateTimeKind.Utc);
        sut.Gauge(key, 3, unit, tags, time1);

        DateTime time2 = new(1970, 1, 1, 0, 0, 38, 0, DateTimeKind.Utc);
        sut.Gauge(key, 5, unit, tags, time2);

        DateTime time3 = new(1970, 1, 1, 0, 0, 40, 0, DateTimeKind.Utc);
        sut.Gauge(key, 13, unit, tags, time3);

        // Assert
        var bucket1 = sut.Buckets[MetricAggregator.GetTimeBucketKey(time1)];
        var data1 = (GaugeMetric)bucket1[MetricAggregator.GetMetricBucketKey(metricType, key, unit, tags)];
        data1.Value.Should().Be(5);
        data1.First.Should().Be(3);
        data1.Min.Should().Be(3);
        data1.Max.Should().Be(5);
        data1.Sum.Should().Be(8);
        data1.Count.Should().Be(2);

        var bucket2 = sut.Buckets[MetricAggregator.GetTimeBucketKey(time3)];
        var data2 = (GaugeMetric)bucket2[MetricAggregator.GetMetricBucketKey(metricType, key, unit, tags)];
        data2.Value.Should().Be(13);
        data2.First.Should().Be(13);
        data2.Min.Should().Be(13);
        data2.Max.Should().Be(13);
        data2.Sum.Should().Be(13);
        data2.Count.Should().Be(1);
    }
}
