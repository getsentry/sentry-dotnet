using Sentry.AspNet;

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
        transaction.NameSource.Should().Be(TransactionNameSource.Url);
        transaction.Operation.Should().Be("http.server");
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
}
