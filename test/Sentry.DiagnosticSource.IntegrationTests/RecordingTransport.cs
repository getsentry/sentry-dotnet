internal class RecordingTransport : ITransport
{
    private List<Envelope> _envelopes = new();

    public IEnumerable<Envelope> Envelopes => _envelopes;

    public Task SendEnvelopeAsync(Envelope envelope, CancellationToken cancellationToken = default)
    {
        lock (_envelopes)
        {
            _envelopes.Add(envelope);
        }
        return Task.CompletedTask;
    }
}
