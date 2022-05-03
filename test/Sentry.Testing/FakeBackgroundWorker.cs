namespace Sentry.Testing;

public class FakeBackgroundWorker : IBackgroundWorker
{
    private readonly List<Envelope> _queue = new();

    public int QueuedItems => _queue.Count;

    public bool EnqueueEnvelope(Envelope envelope)
    {
        _queue.Add(envelope);
        return true;
    }

    public Task FlushAsync(TimeSpan timeout) => Task.CompletedTask;
}
