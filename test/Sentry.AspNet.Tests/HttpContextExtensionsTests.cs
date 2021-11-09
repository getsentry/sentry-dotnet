using System.Web;

namespace Sentry.AspNet.Tests;

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
            new HttpResponse(TextWriter.Null));

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
        using var _ = SentrySdk.UseHub(new Sentry.Internal.Hub(
            new SentryOptions { Dsn = "https://d4d82fc1c2c4032a83f3a29aa3a3aff@fake-sentry.io:65535/2147483647" },
            Substitute.For<ISentryClient>()
        ));

        var context = new HttpContext(
            new HttpRequest("foo", "https://localhost/person/13", "details=true")
            {
                RequestType = "GET"
            },
            new HttpResponse(TextWriter.Null));

        // Act
        var transaction = context.StartSentryTransaction();
        var transactionFromScope = SentrySdk.GetSpan();

        // Assert
        transactionFromScope.Should().BeSameAs(transaction);
    }

    [Fact]
    public void FinishSentryTransaction_FinishesTransaction()
    {
        // Arrange
        using var _ = SentrySdk.UseHub(new Sentry.Internal.Hub(
            new SentryOptions { Dsn = "https://d4d82fc1c2c4032a83f3a29aa3a3aff@fake-sentry.io:65535/2147483647" },
            Substitute.For<ISentryClient>()
        ));

        var context = new HttpContext(
            new HttpRequest("foo", "https://localhost/person/13", "details=true")
            {
                RequestType = "GET"
            },
            new HttpResponse(TextWriter.Null)
            {
                StatusCode = 404
            });

        // Act
        var transaction = context.StartSentryTransaction();
        context.FinishSentryTransaction();

        // Assert
        transaction.IsFinished.Should().BeTrue();
        transaction.Status.Should().Be(SpanStatus.NotFound);
    }
}
