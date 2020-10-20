using System;
using FluentAssertions;
using Sentry.Internal.Http;
using Xunit;

namespace Sentry.Tests.Internals.Http
{
    public class SentryEnvelopeQuotaLimitTests
    {
        [Fact]
        public void Parse_MinimalFormat_Works()
        {
            // Arrange
            const string value = "60:transaction";

            // Act
            var quotaLimit = SentryEnvelopeQuotaLimit.Parse(value);

            // Assert
            quotaLimit.Should().BeEquivalentTo(new SentryEnvelopeQuotaLimit(
                new[] {"transaction"},
                TimeSpan.FromSeconds(60)
            ));
        }

        [Fact]
        public void Parse_FullFormat_Works()
        {
            // Arrange
            const string value = "60:transaction:key";

            // Act
            var quotaLimit = SentryEnvelopeQuotaLimit.Parse(value);

            // Assert
            quotaLimit.Should().BeEquivalentTo(new SentryEnvelopeQuotaLimit(
                new[] {"transaction"},
                TimeSpan.FromSeconds(60)
            ));
        }

        [Fact]
        public void Parse_MultipleCategories_Works()
        {
            // Arrange
            const string value = "2700:default;error;security:organization";

            // Act
            var quotaLimit = SentryEnvelopeQuotaLimit.Parse(value);

            // Assert
            quotaLimit.Should().BeEquivalentTo(new SentryEnvelopeQuotaLimit(
                new[] {"default", "error", "security"},
                TimeSpan.FromSeconds(2700)
            ));
        }
    }
}
