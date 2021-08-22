using System;
using System.Collections.Generic;
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
                SentryTraceHeader.Parse("75302ac48a024bde9a3b3734a82e36c8-1000000000000000-0"));

            using var innerHandler = new RecordingHttpMessageHandler(new FakeHttpMessageHandler());
            using var sentryHandler = new SentryHttpMessageHandler(innerHandler, hub);
            using var client = new HttpClient(sentryHandler);

            // Act
            await client.GetAsync("https://fake.tld/");

            using var request = innerHandler.GetRequests().Single();

            // Assert
            request.Headers.Should().Contain(h =>
                h.Key == "sentry-trace" &&
                string.Concat(h.Value) == "75302ac48a024bde9a3b3734a82e36c8-1000000000000000-0");
        }

        [Fact]
        public async Task SendAsync_SentryTraceHeaderAlreadySet_NotOverwritten()
        {
            // Arrange
            var hub = Substitute.For<IHub>();

            hub.GetTraceHeader().ReturnsForAnyArgs(
                SentryTraceHeader.Parse("75302ac48a024bde9a3b3734a82e36c8-1000000000000000-0"));

            using var innerHandler = new RecordingHttpMessageHandler(new FakeHttpMessageHandler());
            using var sentryHandler = new SentryHttpMessageHandler(innerHandler, hub);
            using var client = new HttpClient(sentryHandler);

            client.DefaultRequestHeaders.Add("sentry-trace", "foobar");

            // Act
            await client.GetAsync("https://fake.tld/");

            using var request = innerHandler.GetRequests().Single();

            // Assert
            request.Headers.Should().Contain(h =>
                h.Key == "sentry-trace" &&
                string.Concat(h.Value) == "foobar");
        }

        [Fact]
        public async Task SendAsync_TransactionOnScope_StartsNewSpan()
        {
            // Arrange
            var hub = Substitute.For<IHub>();

            var transaction = new TransactionTracer(
                hub,
                "foo",
                "bar");

            hub.GetSpan().ReturnsForAnyArgs(transaction);

            using var innerHandler = new FakeHttpMessageHandler();
            using var sentryHandler = new SentryHttpMessageHandler(innerHandler, hub);
            using var client = new HttpClient(sentryHandler);

            // Act
            await client.GetAsync("https://fake.tld/");

            // Assert
            transaction.Spans.Should().Contain(span =>
                span.Operation == "http.client" &&
                span.Description == "GET https://fake.tld/" &&
                span.IsFinished);
        }

        [Fact]
        public async Task SendAsync_ExceptionThrown_ExceptionLinkedToSpan()
        {
            // Arrange
            var hub = Substitute.For<IHub>();

            var transaction = new TransactionTracer(
                hub,
                "foo",
                "bar");

            hub.GetSpan().ReturnsForAnyArgs(transaction);

            var exception = new Exception();

            using var innerHandler = new FakeHttpMessageHandler(() => throw exception);
            using var sentryHandler = new SentryHttpMessageHandler(innerHandler, hub);
            using var client = new HttpClient(sentryHandler);

            // Act
            await Assert.ThrowsAsync<Exception>(() => client.GetAsync("https://fake.tld/"));

            // Assert
            hub.Received(1).BindException(exception, Arg.Any<ISpan>()); // second argument is an implicitly created span
        }

        [Fact]
        public async Task SendAsync_Executed_BreadcrumbCreated()
        {
            // Arrange
            var scope = new Scope();
            var hub = Substitute.For<IHub>();
                hub.When(h => h.ConfigureScope(Arg.Any<Action<Scope>>()))
                   .Do(c => c.Arg<Action<Scope>>()(scope));

            var url = "https://fake.tld/";

            var urlKey = "url";
            var methodKey = "method";
            var statusKey = "status_code";
            var expectedBreadcrumbData = new Dictionary<string, string>
                {
                    { urlKey, url },
                    { methodKey, "GET" },
                    { statusKey, "200" }
                };
            var expectedType = "http";
            var expectedCategory = "http";
            using var sentryHandler = new SentryHttpMessageHandler(hub);
            sentryHandler.InnerHandler = new FakeHttpMessageHandler(); // No reason to reach the Internet here
            using var client = new HttpClient(sentryHandler);

            // Act
            await client.GetAsync(url);
            var breadcrumbGenerated = scope.Breadcrumbs.First();

            // Assert
            Assert.Equal(expectedType, breadcrumbGenerated.Type);
            Assert.Equal(expectedCategory, breadcrumbGenerated.Category);

            Assert.True(breadcrumbGenerated.Data.ContainsKey(urlKey));
            Assert.Equal(expectedBreadcrumbData[urlKey], breadcrumbGenerated.Data[urlKey]);

            Assert.True(breadcrumbGenerated.Data.ContainsKey(methodKey));
            Assert.Equal(expectedBreadcrumbData[methodKey], breadcrumbGenerated.Data[methodKey]);

            Assert.True(breadcrumbGenerated.Data.ContainsKey(statusKey));
            Assert.Equal(expectedBreadcrumbData[statusKey], breadcrumbGenerated.Data[statusKey]);
        }
    }
}
