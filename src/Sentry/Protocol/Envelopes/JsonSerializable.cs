using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Sentry.Protocol.Envelopes
{
    /// <summary>
    /// Represents an object serializable in JSON format.
    /// </summary>
    internal sealed class JsonSerializable : ISerializable
    {
        /// <summary>
        /// Source object.
        /// </summary>
        public IJsonSerializable Source { get; }

        /// <summary>
        /// Initializes an instance of <see cref="JsonSerializable"/>.
        /// </summary>
        public JsonSerializable(IJsonSerializable source) => Source = source;

        /// <inheritdoc />
        public async Task SerializeAsync(Stream stream, CancellationToken cancellationToken = default)
        {
            await using var writer = new Utf8JsonWriter(stream);
            Source.WriteTo(writer);
            await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
