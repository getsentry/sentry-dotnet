namespace Sentry.Tests;

public class TransactionTracerTests
{
    [Fact]
    public void Dispose_Unfinished_Finishes()
    {
        // Arrange
        var hub = Substitute.For<IHub>();
        var transaction = new TransactionTracer(hub, "op", "name");

        // Act
        transaction.Dispose();

        // Assert
        hub.Received(1).CaptureTransaction(Arg.Is<SentryTransaction>(t => t.SpanId == transaction.SpanId));
    }

    [Fact]
    public void Dispose_Finished_DoesNothing()
    {
        // Arrange
        var hub = Substitute.For<IHub>();
        var transaction = new TransactionTracer(hub, "op", "name");
        transaction.Finish();

        // Act
        transaction.Dispose();

        // Assert
        hub.Received(1).CaptureTransaction(Arg.Is<SentryTransaction>(t => t.SpanId == transaction.SpanId));
    }
}
