namespace Sentry.Http;

/// <summary>
/// Transport for sending pre-serialized envelopes to a Spotlight sidecar.
/// Accepts raw bytes (not <see cref="Protocol.Envelopes.Envelope"/> objects) so that
/// serialization can happen synchronously on the caller's thread, eliminating race
/// conditions with the main pipeline that mutates the event after capture.
/// </summary>
internal interface ISpotlightTransport
{
    Task SendAsync(byte[] serializedEnvelope, CancellationToken cancellationToken = default);
}
