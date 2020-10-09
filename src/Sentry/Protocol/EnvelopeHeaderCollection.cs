using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Sentry.Internal;

namespace Sentry.Protocol
{
    /// <summary>
    /// Collection of envelope headers.
    /// </summary>
    public class EnvelopeHeaderCollection : ISerializable
    {
        /// <summary>
        /// Key value pairs.
        /// </summary>
        public IReadOnlyDictionary<string, object> KeyValues { get; }

        /// <summary>
        /// Header count.
        /// </summary>
        public int Count => KeyValues.Count;

        /// <summary>
        /// Initializes an instance of <see cref="EnvelopeHeaderCollection" />.
        /// </summary>
        public EnvelopeHeaderCollection(IReadOnlyDictionary<string, object> keyValues)
        {
            KeyValues = keyValues;
        }

        /// <inheritdoc />
        public async Task SerializeAsync(Stream stream, CancellationToken cancellationToken = default)
        {
            var data = Encoding.UTF8.GetBytes(JsonSerializer.SerializeObject(KeyValues));
            await stream.WriteAsync(data, cancellationToken).ConfigureAwait(false);
        }
    }
}
