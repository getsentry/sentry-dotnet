using System.IO;
using System.Text;
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

    public static class SerializableExtensions
    {
        public static async Task<string> SerializeToStringAsync(
            this ISerializable serializable,
            CancellationToken cancellationToken = default)
        {
            using var stream = new MemoryStream();
            await serializable.SerializeAsync(stream, cancellationToken).ConfigureAwait(false);
            return Encoding.UTF8.GetString(stream.ToArray());
        }
    }
}
