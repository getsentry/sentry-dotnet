namespace Sentry.Testing;

internal class FakeTransportWithRecorder : FakeTransport
{
    private readonly IClientReportRecorder _clientReportRecorder;

    public FakeTransportWithRecorder(IClientReportRecorder clientReportRecorder)
    {
        _clientReportRecorder = clientReportRecorder;
    }

    public override async Task SendEnvelopeAsync(Envelope envelope, CancellationToken cancellationToken = default)
    {
        // Attach a client report in the same way that the HttpTransportBase class does.
        var clientReport = _clientReportRecorder.GenerateClientReport();
        if (clientReport != null)
        {
            var envelopeItems = envelope.Items.ToList();
            envelopeItems.Add(EnvelopeItem.FromClientReport(clientReport));
            envelope = new Envelope(envelope.Header, envelopeItems);
        }

        await base.SendEnvelopeAsync(envelope, cancellationToken).ConfigureAwait(false);
    }
}
