using Sentry.Internal.Tracing;

namespace Sentry.OpenTelemetry.Tests;

public class OpenTelemetryExtensionsTests
{
    [Fact]
    public void BaggageHeader_CreateWithValues_Filters_NullValue_Members()
    {
        var result = new Dictionary<string, string>
        {
            ["a"] = "1",
            ["b"] = null,
            ["c"] = "3"
        }.AsBaggageHeader();

        result.Members.Should().Equal(new Dictionary<string, string>
        {
            ["a"] = "1",
            ["c"] = "3"
        });
    }
}
