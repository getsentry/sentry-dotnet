using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Sentry.Internal;

namespace Sentry.Protocol.Envelopes
{
    /// <summary>
    /// Represents an object serializable in JSON format.
    /// </summary>
    internal class JsonSerializable : ISerializable
    {
        /// <summary>
        /// Source object.
        /// </summary>
        public object Source { get; }

        /// <summary>
        /// Initializes an instance of <see cref="JsonSerializable"/>.
        /// </summary>
        public JsonSerializable(object source) => Source = source;

        /// <inheritdoc />
        public async Task SerializeAsync(Stream stream, CancellationToken cancellationToken = default) =>
            await Json.SerializeToStreamAsync(Source, stream, cancellationToken).ConfigureAwait(false);
    }
}
