using System.IO;
using System.Web;
using FluentAssertions;
using Xunit;

namespace Sentry.AspNet.Tests
{
    public class HttpContextExtensionsTests
    {
        [Fact]
        public void StartSentryTransaction_CreatesValidTransaction()
        {
            // Arrange
            var context = new HttpContext(
                new HttpRequest("foo", "https://localhost/person/13", "details=true")
                {
                    RequestType = "GET"
                },
                new HttpResponse(TextWriter.Null)
            );

            // Act
            var transaction = context.StartSentryTransaction();

            // Assert
            transaction.Name.Should().Be("GET /person/13");
            transaction.Operation.Should().Be("http.server");
        }

        [Fact]
        public void StartSentryTransaction_BindsToScope()
        {
            // Arrange
            var context = new HttpContext(
                new HttpRequest("foo", "https://localhost/person/13", "details=true")
                {
                    RequestType = "GET"
                },
                new HttpResponse(TextWriter.Null)
            );

            // Act
            var transaction = context.StartSentryTransaction();
            var transactionFromScope = SentrySdk.GetSpan();

            // Assert
            transactionFromScope.Should().BeSameAs(transaction);
        }

        [Fact]
        public void StartSentryTransaction_HandlesTraceHeader()
        {
            // Arrange
            var context = new HttpContext(
                new HttpRequest("foo", "https://localhost/person/13", "details=true")
                {
                    RequestType = "GET",
                    Headers =
                    {
                        ["sentry-trace"] = "75302ac48a024bde9a3b3734a82e36c8-1000000000000000-0"
                    }
                },
                new HttpResponse(TextWriter.Null)
            );

            // Act
            var transaction = context.StartSentryTransaction();

            // Assert
            transaction.TraceId.Should().Be(SentryId.Parse("75302ac48a024bde9a3b3734a82e36c8"));
            transaction.ParentSpanId.Should().Be(SpanId.Parse("1000000000000000"));
            transaction.IsSampled.Should().BeFalse();
        }
    }
}
