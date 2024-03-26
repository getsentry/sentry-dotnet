using Sentry.Internal.Tracing;

namespace Sentry.OpenTelemetry.Tests;

public class OpenTelemetryTransactionProcessorTests : ActivitySourceTests
{
    [Fact]
    public void Process_WithActivity_SetsTraceAndSpanIds()
    {
        // Arrange
        using var activity = Tracer.StartActivity("Parent");
        var transaction = new SentryTransaction("name", "operation");
        var processor = new OpenTelemetryTransactionProcessor();

        // Act
        var processedTransaction = processor.Process(transaction);

        // Assert
        processedTransaction.Contexts.Trace.TraceId.Should().Be(activity?.TraceId.AsSentryId());
        processedTransaction.Contexts.Trace.SpanId.Should().Be(activity?.SpanId.AsSentrySpanId());
    }

    [Fact]
    public void Process_WithoutActivity_DoesNotModifyTransaction()
    {
        // Arrange
        var transaction = new SentryTransaction("name", "operation");
        var previousTraceId = transaction.Contexts.Trace.TraceId;
        var previousSpanId = transaction.Contexts.Trace.SpanId;
        var processor = new OpenTelemetryTransactionProcessor();

        // Act
        var processedTransaction = processor.Process(transaction);

        // Assert
        Assert.Equal(previousTraceId, processedTransaction.Contexts.Trace.TraceId);
        Assert.Equal(previousSpanId, processedTransaction.Contexts.Trace.SpanId);
    }
}
