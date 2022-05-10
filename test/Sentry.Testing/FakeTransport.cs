using System.Collections.Concurrent;

namespace Sentry.Testing;

internal class FakeTransport : ITransport, IDisposable
{
    private readonly TimeSpan _artificialDelay;
    private readonly ConcurrentQueue<Envelope> _envelopes = new();

    public event EventHandler<Envelope> EnvelopeSent;

    public FakeTransport(TimeSpan artificialDelay = default)
    {
        _artificialDelay = artificialDelay;
    }

    public virtual async Task SendEnvelopeAsync(
        Envelope envelope,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (_artificialDelay > TimeSpan.Zero)
        {
            await Task.Delay(_artificialDelay, CancellationToken.None);
        }

        _envelopes.Enqueue(envelope);
        EnvelopeSent?.Invoke(this, envelope);
    }

    public IReadOnlyList<Envelope> GetSentEnvelopes() => _envelopes.ToArray();

    public void Dispose() => _envelopes.DisposeAll();
}
