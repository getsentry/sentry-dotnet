using HttpCookie = System.Web.HttpCookie;

namespace Sentry.AspNet.Tests;

public class HttpContextExtensionsTests
{
    [Fact]
    public void StartSentryTransaction_CreatesValidTransaction()
    {
        // Arrange
        var context = HttpContextBuilder.Build();

        // Act
        var transaction = context.StartSentryTransaction();

        // Assert
        transaction.Name.Should().Be("GET /the/path");
        transaction.Operation.Should().Be("http.server");
        transaction.NameSource.Should().Be(TransactionNameSource.Url);
        transaction.Contexts.Trace.Origin.Should().Be(HttpContextExtensions.AspNetOrigin);
    }

    [Fact]
    public void StartSentryTransaction_BindsToScope()
    {
        // Arrange
        using var _ = SentrySdk.UseHub(new Hub(
            new SentryOptions
            {
                Dsn = ValidDsn
            },
            Substitute.For<ISentryClient>()
        ));

        var context = HttpContextBuilder.Build();

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
        using var _ = SentrySdk.UseHub(new Hub(
            new SentryOptions
            {
                Dsn = ValidDsn
            },
            Substitute.For<ISentryClient>()
        ));

        var context = HttpContextBuilder.Build(404);

        // Act
        var transaction = context.StartSentryTransaction();
        context.FinishSentryTransaction();

        // Assert
        transaction.IsFinished.Should().BeTrue();
        transaction.Status.Should().Be(SpanStatus.NotFound);
    }

    [Fact]
    public void StartSentryTransaction_SendDefaultPii_set_to_true_sets_cookies()
    {
        // Arrange
        var context = HttpContextBuilder.BuildWithCookies(new[] { new HttpCookie("foo", "bar") });

        SentryClientExtensions.SentryOptionsForTestingOnly = new SentryOptions
        {
            SendDefaultPii = true
        };

        // Act
        var transaction = context.StartSentryTransaction();

        // Assert
        transaction.Request.Cookies.Should().Be("foo=bar");
    }

    [Fact]
    public void StartSentryTransaction_SendDefaultPii_set_to_true_does_not_set_cookies_if_none_found()
    {
        // Arrange
        var context = HttpContextBuilder.BuildWithCookies(new HttpCookie[] { });

        SentryClientExtensions.SentryOptionsForTestingOnly = new SentryOptions
        {
            SendDefaultPii = true
        };

        // Act
        var transaction = context.StartSentryTransaction();

        // Assert
        transaction.Request.Cookies.Should().BeEmpty();
    }

    [Fact]
    public void StartSentryTransaction_SendDefaultPii_set_to_false_does_not_set_cookies()
    {
        // Arrange
        var context = HttpContextBuilder.BuildWithCookies(new[] { new HttpCookie("foo", "bar") });

        SentryClientExtensions.SentryOptionsForTestingOnly = new SentryOptions
        {
            SendDefaultPii = false
        };

        // Act
        var transaction = context.StartSentryTransaction();

        // Assert
        transaction.Request.Cookies.Should().BeNull();
    }

    [Fact]
    public void StartSentryTransaction_missing_options_does_not_set_cookies()
    {
        // Arrange
        var context = HttpContextBuilder.BuildWithCookies(new[] { new HttpCookie("foo", "bar") });

        // Act
        var transaction = context.StartSentryTransaction();

        // Assert
        transaction.Request.Cookies.Should().BeNull();
    }
}
