using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Sentry.Testing;
using Xunit;

namespace Sentry.Tests
{
    public class SentryHttpMessageHandlerTests
    {
        [Fact]
        public async Task SendAsync_SentryTraceHeaderNotSet_SetsHeader()
        {
            // Arrange
            var hub = Substitute.For<IHub>();

            hub.GetTraceHeader().ReturnsForAnyArgs(
                SentryTraceHeader.Parse("75302ac48a024bde9a3b3734a82e36c8-1000000000000000-0")
            );

            using var innerHandler = new RecordingHttpMessageHandler(new FakeHttpMessageHandler());
            using var sentryHandler = new SentryHttpMessageHandler(innerHandler, hub);
            using var client = new HttpClient(sentryHandler);

            // Act
            await client.GetAsync("https://example.com/");

            using var request = innerHandler.GetRequests().Single();

            // Assert
            request.Headers.Should().Contain(h =>
                h.Key == "sentry-trace" &&
                string.Concat(h.Value) == "75302ac48a024bde9a3b3734a82e36c8-1000000000000000-0"
            );
        }

        [Fact]
        public async Task SendAsync_SentryTraceHeaderAlreadySet_NotOverwritten()
        {
            // Arrange
            var hub = Substitute.For<IHub>();

            hub.GetTraceHeader().ReturnsForAnyArgs(
                SentryTraceHeader.Parse("75302ac48a024bde9a3b3734a82e36c8-1000000000000000-0")
            );

            using var innerHandler = new RecordingHttpMessageHandler(new FakeHttpMessageHandler());
            using var sentryHandler = new SentryHttpMessageHandler(innerHandler, hub);
            using var client = new HttpClient(sentryHandler);

            client.DefaultRequestHeaders.Add("sentry-trace", "foobar");

            // Act
            await client.GetAsync("https://example.com/");

            using var request = innerHandler.GetRequests().Single();

            // Assert
            request.Headers.Should().Contain(h =>
                h.Key == "sentry-trace" &&
                string.Concat(h.Value) == "foobar"
            );
        }

        [Fact]
        public async Task SendAsync_TransactionOnScope_StartsNewSpan()
        {
            // Arrange
            var hub = Substitute.For<IHub>();

            var transaction = new Transaction(
                Substitute.For<ISentryClient>(),
                "foo",
                "bar"
            );

            hub.GetSpan().ReturnsForAnyArgs(transaction);

            using var innerHandler = new FakeHttpMessageHandler();
            using var sentryHandler = new SentryHttpMessageHandler(innerHandler, hub);
            using var client = new HttpClient(sentryHandler);

            // Act
            await client.GetAsync("https://example.com/");

            // Assert
            transaction.Spans.Should().Contain(span =>
                span.Operation == "http.client" &&
                span.Description == "GET https://example.com/" &&
                span.IsFinished
            );
        }
    }
}
