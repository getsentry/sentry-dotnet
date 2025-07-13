namespace Sentry.Tests.Internals;

public class UnsampledSpanTests
{
    [Fact]
    public void GetTraceHeader_CreatesHeaderFromUnsampledTransaction()
    {
        // Arrange
        var hub = Substitute.For<IHub>();
        ITransactionContext context = new TransactionContext("TestTransaction", "TestOperation",
            new SentryTraceHeader(SentryId.Create(), SpanId.Create(), false)
        );
        var transaction = new UnsampledTransaction(hub, context);
        var unsampledSpan = transaction.StartChild("Foo");

        // Act
        var traceHeader = unsampledSpan.GetTraceHeader();

        // Assert
        traceHeader.TraceId.Should().Be(context.TraceId);
        traceHeader.SpanId.Should().Be(context.SpanId);
        traceHeader.IsSampled.Should().Be(context.IsSampled);
    }
}
