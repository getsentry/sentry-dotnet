#nullable enable

namespace Sentry.Tests;

public class HubExtensionsRecordTests
{
    private readonly IHub _hub = Substitute.For<IHub>();
    private SentryTransaction? _captured;

    public HubExtensionsRecordTests()
    {
        // With a substitute hub, GetSentryOptions() returns null, so RecordTransaction falls back to the
        // single-argument CaptureTransaction overload.
        _hub.When(h => h.CaptureTransaction(Arg.Any<SentryTransaction>()))
            .Do(ci => _captured = ci.Arg<SentryTransaction>());
        _hub.When(h => h.CaptureTransaction(Arg.Any<SentryTransaction>(), Arg.Any<Scope>(), Arg.Any<SentryHint>()))
            .Do(ci => _captured = ci.Arg<SentryTransaction>());
    }

    [Fact]
    public void RecordTransaction_SetsNameOperationAndTiming()
    {
        var start = new DateTimeOffset(2023, 09, 28, 10, 00, 00, TimeSpan.Zero);
        var duration = TimeSpan.FromSeconds(5);

        var eventId = _hub.RecordTransaction("my-transaction", "my-op", start, duration);

        var transaction = Assert.IsType<SentryTransaction>(_captured);
        Assert.Equal(eventId, transaction.EventId);
        Assert.Equal("my-transaction", transaction.Name);
        Assert.Equal("my-op", transaction.Operation);
        Assert.Equal(start, transaction.StartTimestamp);
        Assert.Equal(start + duration, transaction.EndTimestamp);
        Assert.True(transaction.IsFinished);
        Assert.True(transaction.IsSampled);
        Assert.Equal(SpanStatus.Ok, transaction.Status);
    }

    [Fact]
    public void RecordTransaction_PreservesTraceAndSpanIds()
    {
        var traceId = SentryId.Create();
        var spanId = SpanId.Create();
        var parentSpanId = SpanId.Create();

        _hub.RecordTransaction("t", "op", DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1),
            traceId: traceId, spanId: spanId, parentSpanId: parentSpanId);

        var transaction = Assert.IsType<SentryTransaction>(_captured);
        Assert.Equal(traceId, transaction.TraceId);
        Assert.Equal(spanId, transaction.SpanId);
        Assert.Equal(parentSpanId, transaction.ParentSpanId);
    }

    [Fact]
    public void RecordTransaction_AppliesMetadata()
    {
        _hub.RecordTransaction("t", "op", DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1), configure: tx =>
        {
            tx.Release = "1.2.3";
            tx.Environment = "staging";
            tx.Description = "recorded elsewhere";
            tx.Status = SpanStatus.InternalError;
            tx.SetTag("origin", "proxy");
            tx.SetData("count", 42);
        });

        var transaction = Assert.IsType<SentryTransaction>(_captured);
        Assert.Equal("1.2.3", transaction.Release);
        Assert.Equal("staging", transaction.Environment);
        Assert.Equal("recorded elsewhere", transaction.Description);
        Assert.Equal(SpanStatus.InternalError, transaction.Status);
        Assert.Equal("proxy", transaction.Tags["origin"]);
        Assert.Equal(42, transaction.Data["count"]);
    }

    [Fact]
    public void RecordTransaction_RecordsNestedSpanTree()
    {
        var start = new DateTimeOffset(2023, 09, 28, 10, 00, 00, TimeSpan.Zero);
        var traceId = SentryId.Create();
        var rootSpanId = SpanId.Create();
        var childSpanId = SpanId.Create();
        var grandchildSpanId = SpanId.Create();

        _hub.RecordTransaction("t", "root-op", start, TimeSpan.FromSeconds(10),
            traceId: traceId, spanId: rootSpanId, configure: tx =>
            {
                tx.RecordSpan("child-op", start.AddSeconds(1), TimeSpan.FromSeconds(2), spanId: childSpanId, configure: child =>
                {
                    child.Description = "child";
                    child.RecordSpan("grandchild-op", start.AddSeconds(1.5), TimeSpan.FromSeconds(0.5), spanId: grandchildSpanId);
                });
            });

        var transaction = Assert.IsType<SentryTransaction>(_captured);
        Assert.Equal(2, transaction.Spans.Count);

        var child = transaction.Spans.Single(s => s.Operation == "child-op");
        Assert.Equal(childSpanId, child.SpanId);
        Assert.Equal(rootSpanId, child.ParentSpanId); // structural parent = transaction root
        Assert.Equal(traceId, child.TraceId); // trace id inherited
        Assert.Equal("child", child.Description);
        Assert.Equal(start.AddSeconds(1), child.StartTimestamp);
        Assert.Equal(start.AddSeconds(3), child.EndTimestamp);

        var grandchild = transaction.Spans.Single(s => s.Operation == "grandchild-op");
        Assert.Equal(grandchildSpanId, grandchild.SpanId);
        Assert.Equal(childSpanId, grandchild.ParentSpanId); // nested under child
        Assert.Equal(traceId, grandchild.TraceId);
    }

    [Fact]
    public void RecordTransaction_ConfigureScope_AppliesToCaptureScope()
    {
        // With options available, RecordTransaction captures against a fresh scope (via the 3-arg overload)
        // that ConfigureScope can mutate — rather than the current live scope.
        var previous = SentryClientExtensions.SentryOptionsForTestingOnly;
        SentryClientExtensions.SentryOptionsForTestingOnly = new SentryOptions();
        try
        {
            Scope? capturedScope = null;
            _hub.When(h => h.CaptureTransaction(Arg.Any<SentryTransaction>(), Arg.Any<Scope>(), Arg.Any<SentryHint>()))
                .Do(ci => capturedScope = ci.Arg<Scope>());

            _hub.RecordTransaction("t", "op", DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1), configure: tx =>
                tx.ConfigureScope(s => s.SetTag("origin", "proxy")));

            Assert.NotNull(capturedScope);
            Assert.Equal("proxy", capturedScope!.Tags["origin"]);
        }
        finally
        {
            SentryClientExtensions.SentryOptionsForTestingOnly = previous;
        }
    }

    [Fact]
    public void RecordTransaction_NegativeDuration_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _hub.RecordTransaction("t", "op", DateTimeOffset.UtcNow, TimeSpan.FromSeconds(-1)));
    }

    [Fact]
    public void RecordSpan_NegativeDuration_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _hub.RecordTransaction("t", "op", DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1), configure: tx =>
                tx.RecordSpan("child", DateTimeOffset.UtcNow, TimeSpan.FromSeconds(-1))));
    }
}
