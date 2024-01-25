using Sentry.Protocol.Metrics;

namespace Sentry.Tests;

public class MetricAggregatorTests
{
    class Fixture
    {
        public SentryOptions Options { get; set; } = new();
        public IHub Hub { get; set; } = Substitute.For<IHub>();
        public IMetricCollector MetricCollector { get; set; } = Substitute.For<IMetricCollector>();
        public bool DisableFlushLoop { get; set; } = true;
        public TimeSpan? FlushInterval { get; set; }
        public MetricAggregator GetSut()
            => new(Options, Hub, MetricCollector, disableLoopTask: DisableFlushLoop, flushInterval: FlushInterval);
    }

    // private readonly Fixture _fixture = new();
    private static readonly Fixture _fixture = new();

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
        result.Should().Be("c_quibbles_none_tag1=value1");
    }

    [Fact]
    public void Increment_AggregatesMetrics()
    {
        // Arrange
        var metricType = MetricType.Counter;
        var key = "counter_key";
        var unit = MeasurementUnit.None;
        var tags = new Dictionary<string, string> { ["tag1"] = "value1" };
        var sut = _fixture.GetSut();

        // Act
        DateTimeOffset firstTime = new(1970, 1, 1, 0, 0, 31, 0, TimeSpan.Zero);
        sut.Increment(key, 3, unit, tags, firstTime);

        DateTimeOffset secondTime = new(1970, 1, 1, 0, 0, 38, 0, TimeSpan.Zero);
        sut.Increment(key, 5, unit, tags, secondTime);

        DateTimeOffset thirdTime = new(1970, 1, 1, 0, 0, 40, 0, TimeSpan.Zero);
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
        var metricType = MetricType.Gauge;
        var key = "gauge_key";
        var unit = MeasurementUnit.None;
        var tags = new Dictionary<string, string> { ["tag1"] = "value1" };
        var sut = _fixture.GetSut();

        // Act
        DateTimeOffset time1 = new(1970, 1, 1, 0, 0, 31, 0, TimeSpan.Zero);
        sut.Gauge(key, 3, unit, tags, time1);

        DateTimeOffset time2 = new(1970, 1, 1, 0, 0, 38, 0, TimeSpan.Zero);
        sut.Gauge(key, 5, unit, tags, time2);

        DateTimeOffset time3 = new(1970, 1, 1, 0, 0, 40, 0, TimeSpan.Zero);
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
        var metricType = MetricType.Distribution;
        var key = "distribution_key";
        var unit = MeasurementUnit.None;
        var tags = new Dictionary<string, string> { ["tag1"] = "value1" };
        var sut = _fixture.GetSut();

        // Act
        DateTimeOffset time1 = new(1970, 1, 1, 0, 0, 31, 0, TimeSpan.Zero);
        sut.Distribution(key, 3, unit, tags, time1);

        DateTimeOffset time2 = new(1970, 1, 1, 0, 0, 38, 0, TimeSpan.Zero);
        sut.Distribution(key, 5, unit, tags, time2);

        DateTimeOffset time3 = new(1970, 1, 1, 0, 0, 40, 0, TimeSpan.Zero);
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
        var metricType = MetricType.Set;
        var key = "set_key";
        var unit = MeasurementUnit.None;
        var tags = new Dictionary<string, string> { ["tag1"] = "value1" };
        var sut = _fixture.GetSut();

        // Act
        DateTimeOffset time1 = new(1970, 1, 1, 0, 0, 31, 0, TimeSpan.Zero);
        sut.Set(key, 3, unit, tags, time1);

        DateTimeOffset time2 = new(1970, 1, 1, 0, 0, 38, 0, TimeSpan.Zero);
        sut.Set(key, 5, unit, tags, time2);

        DateTimeOffset time3 = new(1970, 1, 1, 0, 0, 40, 0, TimeSpan.Zero);
        sut.Set(key, 13, unit, tags, time3);

        DateTimeOffset time4 = new(1970, 1, 1, 0, 0, 42, 0, TimeSpan.Zero);
        sut.Set(key, 13, unit, tags, time3);

        // Assert
        var bucket1 = sut.Buckets[time1.GetTimeBucketKey()];
        var data1 = (SetMetric)bucket1[MetricAggregator.GetMetricBucketKey(metricType, key, unit, tags)];
        data1.Value.Should().BeEquivalentTo(new[] {3, 5});

        var bucket2 = sut.Buckets[time3.GetTimeBucketKey()];
        var data2 = (SetMetric)bucket2[MetricAggregator.GetMetricBucketKey(metricType, key, unit, tags)];
        data2.Value.Should().BeEquivalentTo(new[] {13});
    }

    [Fact]
    public async Task GetFlushableBuckets_IsThreadsafe()
    {
        // Arrange
        const int numThreads = 100;
        const int numThreadIterations = 1000;
        var sent = 0;
        MetricHelper.FlushShift = 0.0;
        _fixture.DisableFlushLoop = false;
        _fixture.FlushInterval = TimeSpan.FromMilliseconds(100);
        _fixture.MetricCollector.CaptureMetrics(Arg.Do<IEnumerable<Metric>>(metrics =>
            {
                foreach (var metric in metrics)
                {
                    Interlocked.Add(ref sent, (int)((CounterMetric)metric).Value);
                }
            }
        ));
        var sut = _fixture.GetSut();

        // Act... spawn some threads that add loads of metrics
        var resetEvent = new ManualResetEvent(false);
        var toProcess = numThreads;
        for(var i = 0; i < numThreads; i++)
        {
            new Thread(delegate()
            {
                for(var i = 0; i < numThreadIterations; i++)
                {
                    sut.Increment("counter");
                }
                // If we're the last thread, signal
                if (Interlocked.Decrement(ref toProcess) == 0)
                {
                    resetEvent.Set();
                }
            }).Start();
        }

        // Wait for workers.
        resetEvent.WaitOne();
        await sut.FlushAsync();

        // Assert
        sent.Should().Be(numThreads * numThreadIterations);
    }

    [Fact]
    public void TestGetCodeLocation()
    {
        // Arrange
        _fixture.Options.StackTraceMode = StackTraceMode.Enhanced;
        var sut = _fixture.GetSut();

        // Act
        var result = sut.GetCodeLocation(1);

        // Assert
        result.Should().NotBeNull();
        result!.Function.Should().Be($"void {nameof(MetricAggregatorTests)}.{nameof(TestGetCodeLocation)}()");
    }

    [Fact]
    public void GetTagsKey_ReturnsEmpty_WhenTagsIsNull()
    {
        var result = MetricAggregator.GetTagsKey(null);
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetTagsKey_ReturnsEmpty_WhenTagsIsEmpty()
    {
        var result = MetricAggregator.GetTagsKey(new Dictionary<string, string>());
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetTagsKey_ReturnsValidString_WhenTagsHasOneEntry()
    {
        var tags = new Dictionary<string, string> { { "tag1", "value1" } };
        var result = MetricAggregator.GetTagsKey(tags);
        result.Should().Be("tag1=value1");
    }

    [Fact]
    public void GetTagsKey_ReturnsCorrectString_WhenTagsHasMultipleEntries()
    {
        var tags = new Dictionary<string, string> { { "tag1", "value1" }, { "tag2", "value2" } };
        var result = MetricAggregator.GetTagsKey(tags);
        result.Should().Be("tag1=value1,tag2=value2");
    }

    [Fact]
    public void GetTagsKey_EscapesCharacters_WhenTagsContainDelimiters()
    {
        var tags = new Dictionary<string, string> { { "tag1\\", "value1\\" }, { "tag2,", "value2," }, { "tag3=", "value3=" } };
        var result = MetricAggregator.GetTagsKey(tags);
        result.Should().Be(@"tag1\\=value1\\,tag2\,=value2\,,tag3\==value3\=");
    }
}
