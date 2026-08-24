using Sentry.Internal.Http;

namespace Sentry.Tests.Internals.Http;

public class RateLimitCategoryTests
{
    [Theory]
    // Rate limits are keyed by data category, which is not always the envelope item type
    [InlineData("error", EnvelopeItem.TypeValueEvent)]
    [InlineData("monitor", EnvelopeItem.TypeValueCheckIn)]
    [InlineData("log_item", EnvelopeItem.TypeValueLog)]
    [InlineData("trace_metric", EnvelopeItem.TypeValueTraceMetric)]
    [InlineData("metric_bucket", EnvelopeItem.TypeValueMetric)]
    [InlineData("feedback", EnvelopeItem.TypeValueFeedback)]
    [InlineData("session", EnvelopeItem.TypeValueSession)]
    [InlineData("transaction", EnvelopeItem.TypeValueTransaction)]
    [InlineData("attachment", EnvelopeItem.TypeValueAttachment)]
    [InlineData("profile", EnvelopeItem.TypeValueProfile)]
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
    // The rate limit category is the data category, not the envelope item type: an "event" item is
    // limited by the "error" category, so the literal item types below must not match.
    [InlineData("event", EnvelopeItem.TypeValueEvent)]
    [InlineData("check_in", EnvelopeItem.TypeValueCheckIn)]
    [InlineData("statsd", EnvelopeItem.TypeValueMetric)]
    [InlineData("error", EnvelopeItem.TypeValueTransaction)]
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
