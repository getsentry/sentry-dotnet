using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Sentry.Protocol
{
    /// <summary>
    /// Represents a serializable entity.
    /// </summary>
    public interface ISerializable
    {
        /// <summary>
        /// Serializes the object to a stream.
        /// </summary>
        Task SerializeAsync(Stream stream, CancellationToken cancellationToken = default);
    }
}
