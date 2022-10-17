namespace Sentry.Testing;

public class FakeBackgroundWorker : IBackgroundWorker
{
    private readonly List<Envelope> _envelopes = new();

    public IEnumerable<Envelope> Envelopes => _envelopes;
    public int QueuedItems => _envelopes.Count;

    public bool EnqueueEnvelope(Envelope envelope)
    {
        _envelopes.Add(envelope);
        return true;
    }

    public Task FlushAsync(TimeSpan timeout) => Task.CompletedTask;
}
