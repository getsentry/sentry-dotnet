namespace Sentry.Tests.Internals.Extensions;

public class ClientReportExtensionsTests
{
    [Fact]
    public void EnvelopeContainsTransaction_ReportsDroppedSpans()
    {
        // Arrange
        var recorder = Substitute.For<IClientReportRecorder>();
        var hub = Substitute.For<IHub>();
        var tracer = new TransactionTracer(hub, "name", "op");
        var span1 = (SpanTracer)tracer.StartChild(null, tracer.SpanId, "span1");
        tracer.StartChild(null, span1.SpanId, "span2");
        tracer.StartChild(null, tracer.SpanId, "span3");
        var transaction = new SentryTransaction(tracer);
        var envelope = Envelope.FromTransaction(transaction);

        // Act
        recorder.RecordDiscardedEvents(DiscardReason.EventProcessor, envelope);

        // Assert
        recorder.Received(1).RecordDiscardedEvent(DiscardReason.EventProcessor, DataCategory.Transaction, 1);
        // 1 for each span + 1 for the transaction root span
        recorder.Received(1).RecordDiscardedEvent(DiscardReason.EventProcessor, DataCategory.Span, transaction.Spans.Count + 1);
    }
}
