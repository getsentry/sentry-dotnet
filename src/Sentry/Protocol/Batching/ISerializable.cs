using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Sentry.Protocol.Batching
{
    /// <summary>
    /// Represents a serializable entity.
    /// </summary>
    internal interface ISerializable
    {
        /// <summary>
        /// Serializes the object to a stream.
        /// </summary>
        ValueTask SerializeAsync(Stream stream, CancellationToken cancellationToken = default);
    }
}
