using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Sentry.Extensibility;

namespace Sentry.Protocol.Envelopes
{
    /// <summary>
    /// Represents a serializable entity.
    /// </summary>
    internal interface ISerializable
    {
        /// <summary>
        /// Serializes the object to a stream.
        /// </summary>
        Task SerializeAsync(Stream stream, IDiagnosticLogger? logger, CancellationToken cancellationToken = default);
    }
}
