using Sentry.Protocol.Metrics;

namespace Sentry.Tests;

[UsesVerify]
public class LocalAggregatorTests
{
    [Fact]
    public async Task WriteTo()
    {
        // Arrange
        var sut = new LocalAggregator();
        sut.Add(MetricType.Distribution, "processor.process_batch", 421.0, MeasurementUnit.Duration.Millisecond);
        var success = new Dictionary<string, string>
        {
            { "success", "true" }
        };
        for (int i = 0; i < 3; i++)
        {
            sut.Add(MetricType.Counter, "processor.item_processed", tags: success);
        }
        var failure = new Dictionary<string, string>
        {
            { "success", "false" }
        };
        for (int i = 0; i < 2; i++)
        {
            sut.Add(MetricType.Counter, "processor.item_processed", tags: failure);
        }
        sut.Add(MetricType.Gauge, "processor.peak_memory_usage", 421, MeasurementUnit.Information.Megabyte);

        // Act
        var json = sut.ToJsonString();

        // Assert
        await VerifyJson(json);
    }
}
