using Sentry.Internal.Http;

namespace Sentry.Tests.Internals.Http;

public class RateLimitCategoryTests
{
    [Theory]
    [InlineData("event", EnvelopeItem.TypeValueEvent)]
    [InlineData("metric_bucket", EnvelopeItem.TypeValueMetric)]
    [InlineData("session", EnvelopeItem.TypeValueSession)]
    [InlineData("transaction", EnvelopeItem.TypeValueTransaction)]
    [InlineData("attachment", EnvelopeItem.TypeValueAttachment)]
    [InlineData("", EnvelopeItem.TypeValueEvent)]
    [InlineData("", EnvelopeItem.TypeValueMetric)]
    [InlineData("", EnvelopeItem.TypeValueSession)]
    [InlineData("", EnvelopeItem.TypeValueTransaction)]
    public void Matches_IncludedItemType_ShouldMatch(string categoryName, string itemType)
    {
        // Arrange
        var category = new RateLimitCategory(categoryName);

        var envelopeItem = new EnvelopeItem(
            new Dictionary<string, object> { ["type"] = itemType },
            new EmptySerializable());

        // Act
        var matches = category.Matches(envelopeItem);

        // Assert
        matches.Should().BeTrue();
    }

    [Theory]
    [InlineData("event", EnvelopeItem.TypeValueTransaction)]
    [InlineData("error", EnvelopeItem.TypeValueAttachment)]
    [InlineData("session", EnvelopeItem.TypeValueEvent)]
    [InlineData("metric_bucket", EnvelopeItem.TypeValueSession)]
    public void Matches_NotIncludedItemType_ShouldNotMatch(string categoryName, string itemType)
    {
        // Arrange
        var category = new RateLimitCategory(categoryName);

        var envelopeItem = new EnvelopeItem(
            new Dictionary<string, object> { ["type"] = itemType },
            new EmptySerializable());

        // Act
        var matches = category.Matches(envelopeItem);

        // Assert
        matches.Should().BeFalse();
    }
}
