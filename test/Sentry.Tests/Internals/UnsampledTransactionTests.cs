namespace Sentry.Tests.Internals;

public class UnsampledTransactionTests
{
    [Fact]
    public void StartChild_CreatesSpan_IsTrackedByParent()
    {
        // Arrange
        var hub = Substitute.For<IHub>();
        ITransactionContext context = new TransactionContext("TestTransaction", "TestOperation",
            new SentryTraceHeader(SentryId.Create(), SpanId.Create(), false)
        );
        var transaction = new UnsampledTransaction(hub, context);

        // Act
        var unsampledSpan = transaction.StartChild("Foo");

        // Assert
        var span = Assert.Single(transaction.Spans);
        Assert.Same(unsampledSpan, span);
    }

    [Fact]
    public void Finish_WithTrackedSpans_ClearsTrackedSpans()
    {
        // Arrange
        var hub = Substitute.For<IHub>();
        ITransactionContext context = new TransactionContext("TestTransaction", "TestOperation",
            new SentryTraceHeader(SentryId.Create(), SpanId.Create(), false)
        );
        var transaction = new UnsampledTransaction(hub, context);
        _ = transaction.StartChild("Foo");

        // Act
        transaction.Finish();

        // Assert
        Assert.Empty(transaction.Spans);
    }
}
