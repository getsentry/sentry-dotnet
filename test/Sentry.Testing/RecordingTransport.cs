public class RecordingTransport : ITransport
{
    private ConcurrentBag<Envelope> _envelopes = new();

    public IEnumerable<Envelope> Envelopes => _envelopes;

    public Task SendEnvelopeAsync(Envelope envelope, CancellationToken cancellationToken = default)
    {
        _envelopes.Add(envelope);
        return Task.CompletedTask;
    }
}
