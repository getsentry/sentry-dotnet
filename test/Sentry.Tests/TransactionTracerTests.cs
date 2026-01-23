namespace Sentry.Tests;

public class TransactionTracerTests
{
    [Fact]
    public void StartChild_CreatesSpanTracer_TracksSpans()
    {
        // Arrange
        var hub = Substitute.For<IHub>();
        var transaction = new TransactionTracer(hub, "op", "name");

        // Act
        var span = transaction.StartChild("operation");

        // Assert
        Assert.IsType<SpanTracer>(span);
        Assert.Collection(transaction.Spans,
            element => Assert.Same(span, element));
    }

    [Fact]
    public void Finish_WithTrackedSpans_ClearsTrackedSpans()
    {
        // Arrange
        var hub = Substitute.For<IHub>();
        var transaction = new TransactionTracer(hub, "op", "name");
        _ = transaction.StartChild("operation");

        // Act
        transaction.Finish();

        // Assert
        Assert.Empty(transaction.Spans);
    }
}
