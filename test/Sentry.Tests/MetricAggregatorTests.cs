using Sentry.Protocol.Metrics;

namespace Sentry.Tests;

public class MetricAggregatorTests
{
    class Fixture
    {
        public SentryOptions Options { get; set; } = new();
        public Action<IEnumerable<Metric>> CaptureMetrics { get; set; } = (_ => { });
        public bool DisableFlushLoop { get; set; } = true;
        public MetricAggregator GetSut()
            => new(Options, CaptureMetrics, disableLoopTask: DisableFlushLoop);
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
        var result = timestamp.GetTimeBucketKey();

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
        var sut = _fixture.GetSut();

        // Act
        DateTime firstTime = new(1970, 1, 1, 0, 0, 31, 0, DateTimeKind.Utc);
        sut.Increment(key, 3, unit, tags, firstTime);

        DateTime secondTime = new(1970, 1, 1, 0, 0, 38, 0, DateTimeKind.Utc);
        sut.Increment(key, 5, unit, tags, secondTime);

        DateTime thirdTime = new(1970, 1, 1, 0, 0, 40, 0, DateTimeKind.Utc);
        sut.Increment(key, 13, unit, tags, thirdTime);

        // Assert
        var bucket1 = sut.Buckets[firstTime.GetTimeBucketKey()];
        var data1 = (CounterMetric)bucket1[MetricAggregator.GetMetricBucketKey(metricType, key, unit, tags)];
        data1.Value.Should().Be(8); // First two emits are in the same bucket

        var bucket2 = sut.Buckets[thirdTime.GetTimeBucketKey()];
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
        var sut = _fixture.GetSut();

        // Act
        DateTime time1 = new(1970, 1, 1, 0, 0, 31, 0, DateTimeKind.Utc);
        sut.Gauge(key, 3, unit, tags, time1);

        DateTime time2 = new(1970, 1, 1, 0, 0, 38, 0, DateTimeKind.Utc);
        sut.Gauge(key, 5, unit, tags, time2);

        DateTime time3 = new(1970, 1, 1, 0, 0, 40, 0, DateTimeKind.Utc);
        sut.Gauge(key, 13, unit, tags, time3);

        // Assert
        var bucket1 = sut.Buckets[time1.GetTimeBucketKey()];
        var data1 = (GaugeMetric)bucket1[MetricAggregator.GetMetricBucketKey(metricType, key, unit, tags)];
        data1.Value.Should().Be(5);
        data1.First.Should().Be(3);
        data1.Min.Should().Be(3);
        data1.Max.Should().Be(5);
        data1.Sum.Should().Be(8);
        data1.Count.Should().Be(2);

        var bucket2 = sut.Buckets[time3.GetTimeBucketKey()];
        var data2 = (GaugeMetric)bucket2[MetricAggregator.GetMetricBucketKey(metricType, key, unit, tags)];
        data2.Value.Should().Be(13);
        data2.First.Should().Be(13);
        data2.Min.Should().Be(13);
        data2.Max.Should().Be(13);
        data2.Sum.Should().Be(13);
        data2.Count.Should().Be(1);
    }

    [Fact]
    public void Distribution_AggregatesMetrics()
    {
        // Arrange
        var metricType = MetricAggregator.MetricType.Distribution;
        var key = "distribution_key";
        var unit = MeasurementUnit.None;
        var tags = new Dictionary<string, string> { ["tag1"] = "value1" };
        var sut = _fixture.GetSut();

        // Act
        DateTime time1 = new(1970, 1, 1, 0, 0, 31, 0, DateTimeKind.Utc);
        sut.Distribution(key, 3, unit, tags, time1);

        DateTime time2 = new(1970, 1, 1, 0, 0, 38, 0, DateTimeKind.Utc);
        sut.Distribution(key, 5, unit, tags, time2);

        DateTime time3 = new(1970, 1, 1, 0, 0, 40, 0, DateTimeKind.Utc);
        sut.Distribution(key, 13, unit, tags, time3);

        // Assert
        var bucket1 = sut.Buckets[time1.GetTimeBucketKey()];
        var data1 = (DistributionMetric)bucket1[MetricAggregator.GetMetricBucketKey(metricType, key, unit, tags)];
        data1.Value.Should().BeEquivalentTo(new[] {3, 5});

        var bucket2 = sut.Buckets[time3.GetTimeBucketKey()];
        var data2 = (DistributionMetric)bucket2[MetricAggregator.GetMetricBucketKey(metricType, key, unit, tags)];
        data2.Value.Should().BeEquivalentTo(new[] {13});
    }

    [Fact]
    public void Set_AggregatesMetrics()
    {
        // Arrange
        var metricType = MetricAggregator.MetricType.Set;
        var key = "set_key";
        var unit = MeasurementUnit.None;
        var tags = new Dictionary<string, string> { ["tag1"] = "value1" };
        var sut = _fixture.GetSut();

        // Act
        DateTime time1 = new(1970, 1, 1, 0, 0, 31, 0, DateTimeKind.Utc);
        sut.Set(key, 3, unit, tags, time1);

        DateTime time2 = new(1970, 1, 1, 0, 0, 38, 0, DateTimeKind.Utc);
        sut.Set(key, 5, unit, tags, time2);

        DateTime time3 = new(1970, 1, 1, 0, 0, 40, 0, DateTimeKind.Utc);
        sut.Set(key, 13, unit, tags, time3);

        DateTime time4 = new(1970, 1, 1, 0, 0, 42, 0, DateTimeKind.Utc);
        sut.Set(key, 13, unit, tags, time3);

        // Assert
        var bucket1 = sut.Buckets[time1.GetTimeBucketKey()];
        var data1 = (SetMetric)bucket1[MetricAggregator.GetMetricBucketKey(metricType, key, unit, tags)];
        data1.Value.Should().BeEquivalentTo(new[] {3, 5});

        var bucket2 = sut.Buckets[time3.GetTimeBucketKey()];
        var data2 = (SetMetric)bucket2[MetricAggregator.GetMetricBucketKey(metricType, key, unit, tags)];
        data2.Value.Should().BeEquivalentTo(new[] {13});
    }
}
