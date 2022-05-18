namespace Sentry.Testing;

internal class FakeTransportWithRecorder : FakeTransport
{
    private readonly IClientReportRecorder _recorder;

    public FakeTransportWithRecorder(IClientReportRecorder recorder)
    {
        _recorder = recorder;
    }

    public override async Task SendEnvelopeAsync(Envelope envelope, CancellationToken cancellationToken = default)
    {
        // Attach a client report in the same way that the HttpTransportBase class does.
        var report = _recorder.GenerateClientReport();
        if (report != null)
        {
            envelope = envelope.WithItem(EnvelopeItem.FromClientReport(report));
        }

        try
        {
            await base.SendEnvelopeAsync(envelope, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            // Restore client reports on any failure
            if (report != null)
            {
                _recorder.Load(report);
            }

            throw;
        }
    }
}
