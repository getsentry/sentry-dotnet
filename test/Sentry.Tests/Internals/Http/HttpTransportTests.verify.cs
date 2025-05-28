using Sentry.Http;

namespace Sentry.Tests.Internals.Http;

public partial class HttpTransportTests
{
    [Fact]
    public Task ProcessEnvelope_ShouldAttachClientReport()
    {
        var options = new SentryOptions();

        var recorder = new ClientReportRecorder(options);
        options.ClientReportRecorder = recorder;

        var logger = Substitute.For<IDiagnosticLogger>();

        var httpTransport = Substitute.For<HttpTransportBase>(options, null, null);

        // add some fake discards for the report
        recorder.RecordDiscardedEvent(DiscardReason.NetworkError, DataCategory.Internal);
        recorder.RecordDiscardedEvent(DiscardReason.NetworkError, DataCategory.Security);
        recorder.RecordDiscardedEvent(DiscardReason.QueueOverflow, DataCategory.Error);
        recorder.RecordDiscardedEvent(DiscardReason.QueueOverflow, DataCategory.Error);
        recorder.RecordDiscardedEvent(DiscardReason.RateLimitBackoff, DataCategory.Transaction);
        recorder.RecordDiscardedEvent(DiscardReason.RateLimitBackoff, DataCategory.Transaction);
        recorder.RecordDiscardedEvent(DiscardReason.RateLimitBackoff, DataCategory.Transaction);

        var sentryEvent = new SentryEvent();

        var envelope = Envelope.FromEvent(sentryEvent);
        var processedEnvelope = httpTransport.ProcessEnvelope(envelope);

        // There should be exactly two items in the envelope
        Assert.Equal(2, processedEnvelope.Items.Count);
        var eventItem = processedEnvelope.Items[0];
        var clientReportItem = processedEnvelope.Items[1];

        // Make sure they have the correct types set in their headers
        Assert.Equal("event", eventItem.TryGetType());
        Assert.Equal("client_report", clientReportItem.TryGetType());

        var eventItemJson = eventItem.Payload.SerializeToString(logger);
        var clientReportJson = clientReportItem.Payload.SerializeToString(logger);
        Assert.Contains("timestamp", clientReportJson);
        Assert.Contains("timestamp", eventItemJson);

        return VerifyJson($"{{eventItemJson:{eventItemJson},clientReportJson:{clientReportJson}}}")
            .IgnoreMembers("timestamp");
    }
}
