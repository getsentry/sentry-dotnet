using Sentry.Extensibility;

namespace Sentry.Protocol.Envelopes;

/// <summary>
/// Represents a serializable entity.
/// </summary>
public interface ISerializable
{
    /// <summary>
    /// Serializes the object to a stream asynchronously.
    /// </summary>
    Task SerializeAsync(Stream stream, IDiagnosticLogger? logger, CancellationToken cancellationToken = default);

    /// <summary>
    /// Serializes the object to a stream synchronously.
    /// </summary>
    void Serialize(Stream stream, IDiagnosticLogger? logger);
}
