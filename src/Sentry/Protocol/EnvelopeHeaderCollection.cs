using System.Collections.Generic;
using System.IO;
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
        public async Task SerializeAsync(StreamWriter writer, CancellationToken cancellationToken = default)
        {
            await writer.WriteAsync(JsonSerializer.SerializeObject(KeyValues)).ConfigureAwait(false);
        }
    }
}
