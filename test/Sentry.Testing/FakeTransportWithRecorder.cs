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
            envelope = envelope.WithItem(EnvelopeItem.FromClientReport(clientReport));
        }

        try
        {
            await base.SendEnvelopeAsync(envelope, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            // Restore client reports on any failure
            if (clientReport != null)
            {
                _clientReportRecorder.Load(clientReport);
            }
        }
    }
}
