using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Sentry.Protocol;
using Sentry.Testing;
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

        [Fact]
        public void Inject_ToHttpRequest_Works()
        {
            // Arrange
            using var request = new HttpRequestMessage();
            var header = SentryTraceHeader.Parse("75302ac48a024bde9a3b3734a82e36c8-1000000000000000-0");

            // Act
            header.Inject(request);

            // Assert
            request.Headers.Should().Contain(h =>
                h.Key == "sentry-trace" &&
                string.Concat(h.Value) == "75302ac48a024bde9a3b3734a82e36c8-1000000000000000-0"
            );
        }

        [Fact]
        public async Task Inject_ToHttpClient_Works()
        {
            // Arrange
            using var handler = new FakeHttpClientHandler();
            using var client = new HttpClient(handler);
            var header = SentryTraceHeader.Parse("75302ac48a024bde9a3b3734a82e36c8-1000000000000000-0");

            // Act
            header.Inject(client);
            await client.GetAsync("https://example.com");

            using var request = handler.GetRequests().Single();

            // Assert
            request.Headers.Should().Contain(h =>
                h.Key == "sentry-trace" &&
                string.Concat(h.Value) == "75302ac48a024bde9a3b3734a82e36c8-1000000000000000-0"
            );
        }
    }
}
