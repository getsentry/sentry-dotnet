using ISerializable = Sentry.Protocol.Envelopes.ISerializable;

namespace Sentry.Testing;

public class RecordingTransport : ITransport
{
    private List<Envelope> _envelopes = new();

    public IEnumerable<Envelope> Envelopes => _envelopes;

    public IEnumerable<ISerializable> Payloads =>
        Envelopes
            .SelectMany(x => x.Items)
            .Select(x => x.Payload);

    public Task SendEnvelopeAsync(Envelope envelope, CancellationToken cancellationToken = default)
    {
        lock (_envelopes)
        {
            _envelopes.Add(envelope);
        }

        return Task.CompletedTask;
    }
}
