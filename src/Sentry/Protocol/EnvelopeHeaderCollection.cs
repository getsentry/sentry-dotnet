using System.Collections.Generic;
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
        public string Serialize() => JsonSerializer.SerializeObject(KeyValues);

        /// <inheritdoc />
        public override string ToString() => Serialize();
    }
}
