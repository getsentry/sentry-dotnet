using System.Collections.Generic;
using Sentry.Internal;

namespace Sentry.Protocol
{
    public class EnvelopeHeaderCollection : ISerializable
    {
        public IReadOnlyDictionary<string, object> KeyValues { get; }

        public int Count => KeyValues.Count;

        public EnvelopeHeaderCollection(IReadOnlyDictionary<string, object> keyValues)
        {
            KeyValues = keyValues;
        }

        public string Serialize() => JsonSerializer.SerializeObject(KeyValues);

        public override string ToString() => Serialize();
    }
}
