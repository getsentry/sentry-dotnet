internal class RecordingTransport : ITransport
{
    private List<Envelope> envelopes = new();

    public IEnumerable<Envelope> Envelopes => envelopes;

    public Task SendEnvelopeAsync(Envelope envelope, CancellationToken cancellationToken = default)
    {
        envelopes.Add(envelope);
        return Task.CompletedTask;
    }
}
