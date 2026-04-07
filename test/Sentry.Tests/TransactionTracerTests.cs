using Sentry.Testing;

namespace Sentry.Tests;

public class TransactionTracerTests
{
    private static readonly TimeSpan AnyTimeout = TimeSpan.FromSeconds(30);

    private static (TransactionTracer transaction, MockTimer timer) CreateIdleTransaction(
        IHub hub, string name = "name", string op = "op")
    {
        MockTimer mockTimer = null;
        var transaction = new TransactionTracer(
            hub,
            new TransactionContext(name, op),
            idleTimeout: AnyTimeout,
            timerFactory: cb =>
            {
                mockTimer = new MockTimer(cb);
                return mockTimer;
            });
        return (transaction, mockTimer);
    }

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

    [Fact]
    public void Dispose_WithTrackedSpans_ClearsTrackedSpans()
    {
        // Arrange
        var hub = Substitute.For<IHub>();
        var transaction = new TransactionTracer(hub, "op", "name");
        _ = transaction.StartChild("operation");

        // Act
        transaction.Dispose();

        // Assert
        Assert.Empty(transaction.Spans);
    }

    [Fact]
    public void Finish_ClearsTransactionFromScope()
    {
        // Arrange
        var hub = Substitute.For<IHub>();
        var scope = new Scope();
        hub.SubstituteConfigureScope(scope);

        var transaction = new TransactionTracer(hub, new TransactionContext("name", "op"));
        scope.Transaction = transaction;

        // Act
        transaction.Finish();

        // Assert
        Assert.Null(scope.Transaction);
    }

    // --- Idle timeout scenarios (only apply when idleTimeout is non-null) ---

    [Fact]
    public void IdleTimeout_NoChildSpans_TransactionIsDiscarded()
    {
        // Given an auto-generated UI event transaction with no child spans
        var hub = Substitute.For<IHub>();
        var (_, timer) = CreateIdleTransaction(hub);

        // When the idleTimeout fires
        timer.Fire();

        // Then the SDK discards the transaction (does not capture it)
        hub.DidNotReceive().CaptureTransaction(Arg.Any<SentryTransaction>());
    }

    [Fact]
    public void IdleTimeout_WithFinishedChildSpan_TrimsEndTimestampToLatestSpan()
    {
        // Given an auto-generated UI event transaction with one finished child span
        var hub = Substitute.For<IHub>();
        var (transaction, timer) = CreateIdleTransaction(hub);
        var span = transaction.StartChild("child");
        span.Finish();
        var expectedEndTime = span.EndTimestamp!.Value;

        // When the idleTimeout fires
        timer.Fire();

        // Then the transaction is captured with EndTimestamp trimmed to the last finished span
        hub.Received(1).CaptureTransaction(
            Arg.Is<SentryTransaction>(t => t.EndTimestamp == expectedEndTime));
    }

    [Fact]
    public void StartChild_CancelsIdleTimeout()
    {
        // Given an auto-generated UI event transaction
        var hub = Substitute.For<IHub>();
        var (transaction, timer) = CreateIdleTransaction(hub);
        timer.StartCount.Should().Be(1); // started on creation

        // When the SDK starts a child span
        _ = transaction.StartChild("child");

        // Then the idle timer is cancelled while the span is in flight
        timer.IsCancelled.Should().BeTrue();
    }

    [Fact]
    public void LastSpan_Finish_ResetsIdleTimeout()
    {
        // Given an auto-generated UI event transaction with two child spans
        var hub = Substitute.For<IHub>();
        var (transaction, timer) = CreateIdleTransaction(hub);
        var span1 = transaction.StartChild("child1");
        var span2 = transaction.StartChild("child2");

        // When the first span finishes the timer stays cancelled (span2 still active)
        span1.Finish();
        timer.IsCancelled.Should().BeTrue();

        // When the last span finishes, the idle timer is restarted
        span2.Finish();
        timer.IsCancelled.Should().BeFalse();
        timer.StartCount.Should().Be(2); // initial start + restart after last span
    }

    [Fact]
    public void NonLastSpan_Finish_DoesNotResetIdleTimeout()
    {
        // Given an auto-generated UI event transaction with two child spans
        var hub = Substitute.For<IHub>();
        var (transaction, timer) = CreateIdleTransaction(hub);
        var span1 = transaction.StartChild("child1");
        _ = transaction.StartChild("child2");

        // When the first (non-last) span finishes, the timer is NOT restarted
        span1.Finish();
        timer.IsCancelled.Should().BeTrue();
        timer.StartCount.Should().Be(1); // only the initial start
    }

    [Fact]
    public void LastSpan_Finish_ThenTimerFires_CapturesTransaction()
    {
        // Given an auto-generated UI event transaction
        var hub = Substitute.For<IHub>();
        var (transaction, timer) = CreateIdleTransaction(hub);
        var span = transaction.StartChild("child");
        span.Finish();

        // Timer hasn't fired yet — not captured
        hub.DidNotReceive().CaptureTransaction(Arg.Any<SentryTransaction>());

        // When the idle timer fires
        timer.Fire();

        // Then the transaction is captured
        hub.Received(1).CaptureTransaction(Arg.Any<SentryTransaction>());
    }
}
