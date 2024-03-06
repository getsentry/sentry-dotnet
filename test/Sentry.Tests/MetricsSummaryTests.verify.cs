using System.Collections.Generic;
using System.Threading.Tasks;
using Sentry.Protocol.Metrics;
using Sentry.Testing;
using VerifyXunit;
using Xunit;

namespace Sentry.Tests;

[UsesVerify]
public class MetricsSummaryTests
{
    [Fact]
    public async Task WriteTo()
    {
        // Arrange
        var aggregator = new MetricsSummaryAggregator();
        aggregator.Add(MetricType.Distribution, "processor.process_batch", 421.0, MeasurementUnit.Duration.Millisecond);
        var success = new Dictionary<string, string>
        {
            { "success", "true" }
        };
        for (var i = 0; i < 3; i++)
        {
            aggregator.Add(MetricType.Counter, "processor.item_processed", tags: success);
        }
        var failure = new Dictionary<string, string>
        {
            { "success", "false" }
        };
        for (var i = 0; i < 2; i++)
        {
            aggregator.Add(MetricType.Counter, "processor.item_processed", tags: failure);
        }
        aggregator.Add(MetricType.Gauge, "processor.peak_memory_usage", 421, MeasurementUnit.Information.Megabyte);
        var sut = new MetricsSummary(aggregator);

        // Act
        var json = sut.ToJsonString();

        // Assert
        await Verifier.VerifyJson(json);
    }
}
