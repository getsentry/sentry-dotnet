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
        Task SerializeAsync(StreamWriter writer, CancellationToken cancellationToken = default);
    }

    public static class SerializableExtensions
    {
        public static async Task SerializeAsync(
            this ISerializable serializable,
            Stream stream,
            CancellationToken cancellationToken = default)
        {
            using var writer = new StreamWriter(stream);

            await serializable.SerializeAsync(writer, cancellationToken).ConfigureAwait(false);
        }

        public static async Task<string> SerializeToStringAsync(
            this ISerializable serializable,
            CancellationToken cancellationToken = default)
        {
            using var stream = new MemoryStream();
            using var writer = new StreamWriter(stream);

            await serializable.SerializeAsync(writer, cancellationToken).ConfigureAwait(false);

            return writer.Encoding.GetString(stream.ToArray());
        }
    }
}
