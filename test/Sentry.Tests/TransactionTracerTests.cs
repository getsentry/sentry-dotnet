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

    [Fact]
    public void IdleTimeout_AfterUnfinishedChildSpan_DoesNotCaptureTransaction()
    {
        // Regression: prior to the fix, LastActiveSpanTracker.PeekActive() destructively popped
        // finished spans off the stack. That created a hole when combined with SpanTracer.Unfinish()
        // (used by EF for connection-pool reuse):
        //
        //   1. SpanTracer.Finish() sets EndTimestamp and calls Transaction.ChildSpanFinished().
        //   2. ChildSpanFinished -> PeekActive() destructively pops the now-finished span.
        //   3. Stack empty -> idle timer (re)starts.
        //   4. SpanTracer.Unfinish() resets EndTimestamp = null on the span -- it is logically alive
        //      again, but no longer in _activeSpanTracker.
        //   5. PeekActive() returns null -- the idle timer fires unopposed.
        //   6. TryBeginFinish sees PeekActive() == null and _spans.Count > 0, captures the transaction
        //      while the unfinished span is still in-flight.
        //
        // Fix: PeekActive is non-destructive; an unfinished span stays in the tracker and is
        // re-discoverable after Unfinish().

        // Given an auto-generated UI event transaction with a child span that is finished then unfinished
        var hub = Substitute.For<IHub>();
        var (transaction, timer) = CreateIdleTransaction(hub);
        var span = (SpanTracer)transaction.StartChild("child");
        span.Finish();
        span.Unfinish();

        // When the idle timer fires
        timer.Fire();

        // Then the transaction is NOT captured because the unfinished span is still tracked as active
        hub.DidNotReceive().CaptureTransaction(Arg.Any<SentryTransaction>());
    }
}
