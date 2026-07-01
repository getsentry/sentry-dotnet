using Sentry.Extensibility;
using Sentry.Protocol.Envelopes;

namespace Sentry.Internal.Http;

/// <summary>
/// A transport that discards all envelopes. Used when no DSN is configured (e.g. Spotlight-only mode).
/// </summary>
internal sealed class NoOpTransport : ITransport
{
    public static readonly NoOpTransport Instance = new();

    public Task SendEnvelopeAsync(Envelope envelope, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
