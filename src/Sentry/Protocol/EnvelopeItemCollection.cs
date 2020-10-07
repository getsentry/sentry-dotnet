using System.Collections.Generic;
using System.Linq;

namespace Sentry.Protocol
{
    /// <summary>
    /// Collection of envelope items.
    /// </summary>
    public class EnvelopeItemCollection : ISerializable
    {
        /// <summary>
        /// Items.
        /// </summary>
        public IReadOnlyList<EnvelopeItem> Items { get; }

        /// <summary>
        /// Items count.
        /// </summary>
        public int Count => Items.Count;

        /// <summary>
        /// Initializes an instance of <see cref="EnvelopeItemCollection"/>.
        /// </summary>
        public EnvelopeItemCollection(IReadOnlyList<EnvelopeItem> items)
        {
            Items = items;
        }

        /// <inheritdoc />
        public string Serialize() => string.Join("\n", Items.Select(i => i.Serialize()));

        /// <inheritdoc />
        public override string ToString() => Serialize();
    }
}
