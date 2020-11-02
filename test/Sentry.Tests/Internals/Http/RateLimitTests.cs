using System;
using FluentAssertions;
using Sentry.Internal.Http;
using Xunit;

namespace Sentry.Tests.Internals.Http
{
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
            rateLimit.Should().BeEquivalentTo(new RateLimit(
                new[] {new RateLimitCategory("transaction")},
                TimeSpan.FromSeconds(60)
            ));
        }

        [Fact]
        public void Parse_FullFormat_Works()
        {
            // Arrange
            const string value = "60:transaction:key";

            // Act
            var rateLimit = RateLimit.Parse(value);

            // Assert
            rateLimit.Should().BeEquivalentTo(new RateLimit(
                new[] {new RateLimitCategory("transaction")},
                TimeSpan.FromSeconds(60)
            ));
        }

        [Fact]
        public void Parse_MultipleCategories_Works()
        {
            // Arrange
            const string value = "2700:default;error;security:organization";

            // Act
            var rateLimit = RateLimit.Parse(value);

            // Assert
            rateLimit.Should().BeEquivalentTo(new RateLimit(
                new[]
                {
                    new RateLimitCategory("default"),
                    new RateLimitCategory("error"),
                    new RateLimitCategory("security")
                },
                TimeSpan.FromSeconds(2700)
            ));
        }
    }
}
