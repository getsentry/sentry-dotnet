using Sentry.Protocol.Envelopes;

namespace Sentry.Extensibility;

/// <summary>
/// An abstraction to the transport of the event.
/// </summary>
public interface ITransport
{
    /// <summary>
    /// Sends the <see cref="Envelope" /> to Sentry asynchronously.
    /// </summary>
    /// <param name="envelope">The envelope to send to Sentry.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public Task SendEnvelopeAsync(Envelope envelope, CancellationToken cancellationToken = default);
}
