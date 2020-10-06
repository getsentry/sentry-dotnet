using System.Collections.Generic;
using System.Linq;

namespace Sentry.Protocol
{
    public class EnvelopeItemCollection : ISerializable
    {
        public IReadOnlyList<EnvelopeItem> Items { get; }

        public EnvelopeItemCollection(IReadOnlyList<EnvelopeItem> items)
        {
            Items = items;
        }

        public string Serialize() => string.Join("\n", Items.Select(i => i.Serialize()));

        public override string ToString() => Serialize();
    }
}
