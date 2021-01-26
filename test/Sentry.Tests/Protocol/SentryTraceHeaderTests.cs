using FluentAssertions;
using Sentry.Protocol;
using Xunit;

namespace Sentry.Tests.Protocol
{
    public class SentryTraceHeaderTests
    {
        [Fact]
        public void Parse_WithoutSampled_Works()
        {
            // Arrange
            const string headerValue = "75302ac48a024bde9a3b3734a82e36c8-1000000000000000";

            // Act
            var header = SentryTraceHeader.Parse(headerValue);

            // Assert
            header.TraceId.Should().Be(SentryId.Parse("75302ac48a024bde9a3b3734a82e36c8"));
            header.SpanId.Should().Be(SpanId.Parse("1000000000000000"));
            header.IsSampled.Should().BeNull();
        }

        [Fact]
        public void Parse_WithSampledTrue_Works()
        {
            // Arrange
            const string headerValue = "75302ac48a024bde9a3b3734a82e36c8-1000000000000000-1";

            // Act
            var header = SentryTraceHeader.Parse(headerValue);

            // Assert
            header.TraceId.Should().Be(SentryId.Parse("75302ac48a024bde9a3b3734a82e36c8"));
            header.SpanId.Should().Be(SpanId.Parse("1000000000000000"));
            header.IsSampled.Should().BeTrue();
        }

        [Fact]
        public void Parse_WithSampledFalse_Works()
        {
            // Arrange
            const string headerValue = "75302ac48a024bde9a3b3734a82e36c8-1000000000000000-0";

            // Act
            var header = SentryTraceHeader.Parse(headerValue);

            // Assert
            header.TraceId.Should().Be(SentryId.Parse("75302ac48a024bde9a3b3734a82e36c8"));
            header.SpanId.Should().Be(SpanId.Parse("1000000000000000"));
            header.IsSampled.Should().BeFalse();
        }
    }
}
