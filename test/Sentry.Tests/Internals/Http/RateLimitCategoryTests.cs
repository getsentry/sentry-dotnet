using System.Collections.Generic;
using FluentAssertions;
using Sentry.Internal.Http;
using Sentry.Protocol.Envelopes;
using Sentry.Testing;
using Xunit;

namespace Sentry.Tests.Internals.Http
{
    public class RateLimitCategoryTests
    {
        [Theory]
        [InlineData("event", "event")]
        [InlineData("session", "session")]
        [InlineData("transaction", "transaction")]
        [InlineData("attachment", "attachment")]
        [InlineData("", "event")]
        [InlineData("", "session")]
        [InlineData("", "transaction")]
        public void Matches_IncludedItemType_ShouldMatch(string categoryName, string itemType)
        {
            // Arrange
            var category = new RateLimitCategory(categoryName);

            var envelopeItem = new EnvelopeItem(
                new Dictionary<string, object> {["type"] = itemType},
                new EmptySerializable()
            );

            // Act
            var matches = category.Matches(envelopeItem);

            // Assert
            matches.Should().BeTrue();
        }

        [Theory]
        [InlineData("event", "transaction")]
        [InlineData("error", "attachment")]
        [InlineData("session", "event")]
        public void Matches_NotIncludedItemType_ShouldNotMatch(string categoryName, string itemType)
        {
            // Arrange
            var category = new RateLimitCategory(categoryName);

            var envelopeItem = new EnvelopeItem(
                new Dictionary<string, object> {["type"] = itemType},
                new EmptySerializable()
            );

            // Act
            var matches = category.Matches(envelopeItem);

            // Assert
            matches.Should().BeFalse();
        }
    }
}
