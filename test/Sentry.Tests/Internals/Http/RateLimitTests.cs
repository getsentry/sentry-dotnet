using Sentry.Internal.Http;

namespace Sentry.Tests.Internals.Http;

public class RateLimitTests
{
    [Fact]
    public void Parse_MinimalFormat_Works()
    {
        // Arrange
        const string value = "60:transaction";

        // Act
        var rateLimit = RateLimit.Parse(value);

        // Assert
        rateLimit.Should().BeEquivalentTo(new RateLimit(TimeSpan.FromSeconds(60), new[] { new RateLimitCategory("transaction") }));
    }

    [Fact]
    public void Parse_MinimalFormat_EmptyCatgetory_Works()
    {
        // Arrange
        const string value = "60:";

        // Act
        var rateLimit = RateLimit.Parse(value);

        // Assert
        rateLimit.Should().BeEquivalentTo(new RateLimit(TimeSpan.FromSeconds(60), new[] { new RateLimitCategory("") }));
    }

    [Fact]
    public void Parse_MinimalFormat_EmptyCategory_IgnoresScope()
    {
        // Arrange
        const string value = "60::organization";

        // Act
        var rateLimit = RateLimit.Parse(value);

        // Assert
        rateLimit.Should().BeEquivalentTo(new RateLimit(TimeSpan.FromSeconds(60), new[] { new RateLimitCategory("") }));
    }

    [Fact]
    public void Parse_FullFormat_Works()
    {
        // Arrange
        const string value = "60:transaction:key";

        // Act
        var rateLimit = RateLimit.Parse(value);

        // Assert
        rateLimit.Should().BeEquivalentTo(new RateLimit(TimeSpan.FromSeconds(60), new[] { new RateLimitCategory("transaction") }));
    }

    [Fact]
    public void Parse_MultipleCategories_Works()
    {
        // Arrange
        const string value = "2700:default;error;security:organization";

        // Act
        var rateLimit = RateLimit.Parse(value);

        // Assert
        rateLimit.Should().BeEquivalentTo(new RateLimit(TimeSpan.FromSeconds(2700), new[]
        {
            new RateLimitCategory("default"),
            new RateLimitCategory("error"),
            new RateLimitCategory("security")
        }));
    }

    [Fact]
    public void Parse_SingleNamespace_Works()
    {
        // Arrange
        const string value = "2700:metric_bucket:organization:quota_exceeded:custom";

        // Act
        var rateLimit = RateLimit.Parse(value);

        // Assert
        rateLimit.Should().BeEquivalentTo(new RateLimit(
            TimeSpan.FromSeconds(2700),
            [new RateLimitCategory("metric_bucket")],
            ["custom"]
        ));
    }

    [Fact]
    public void Parse_MultipleNamespaces_Works()
    {
        // Arrange
        const string value = "2700:metric_bucket:organization:quota_exceeded:apples;oranges;pears";

        // Act
        var rateLimit = RateLimit.Parse(value);

        // Assert
        rateLimit.Should().BeEquivalentTo(new RateLimit(
            TimeSpan.FromSeconds(2700),
            [new RateLimitCategory("metric_bucket")],
            ["apples", "oranges", "pears"]
        ));
    }

    [Fact]
    public void Parse_NotMetricBucket_NamespacesIgnored()
    {
        // Arrange
        const string value = "2700:default:organization:quota_exceeded:custom";

        // Act
        var rateLimit = RateLimit.Parse(value);

        // Assert
        rateLimit.Should().BeEquivalentTo(new RateLimit(
            TimeSpan.FromSeconds(2700),
            [new RateLimitCategory("default")]
        ));
    }
}
