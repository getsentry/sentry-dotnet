using Sentry.Protocol.Metrics;
using ISentrySerializable = Sentry.Protocol.Envelopes.ISerializable;

namespace Sentry.Tests;

[UsesVerify]
public class MetricTests
{
    public static IEnumerable<object[]> GetMetrics()
    {
        var timestamp = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var tags = new Dictionary<string, string>
        {
            { "tag1", "value1" },
            { "tag2", "value2" }
        };
        var counter = new CounterMetric("my.counter", 5, MeasurementUnit.Custom("counters"), tags, timestamp);
        yield return new object[] { counter };

        var set = new SetMetric("my.set", 5, MeasurementUnit.Custom("sets"), tags, timestamp);
        set.Add(7);
        yield return new object[] { set };

        var distribution = new DistributionMetric("my.distribution", 5, MeasurementUnit.Custom("distributions"), tags, timestamp);
        distribution.Add(7);
        distribution.Add(13);
        yield return new object[] { distribution };

        var gauge = new GaugeMetric("my.gauge", 5, MeasurementUnit.Custom("gauges"), tags, timestamp);
        gauge.Add(7);
        yield return new object[] { gauge };
    }

    [Theory]
    [MemberData(nameof(GetMetrics))]
    public async Task SerializeAsync_WritesMetric(ISentrySerializable metric)
    {
        // Arrange
        var stream = new MemoryStream();

        // Act
        await metric.SerializeAsync(stream, null);
        stream.Position = 0;
        using var reader = new StreamReader(stream, Encoding.UTF8);
        var statsd = await reader.ReadToEndAsync();

        // Assert
        await Verify(statsd).UseParameters(metric.GetType().Name);
    }
}
