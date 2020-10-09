using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

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
        public async Task SerializeAsync(Stream stream, CancellationToken cancellationToken = default)
        {
            var isFirst = true;

            foreach (var item in Items)
            {
                if (isFirst)
                {
                    isFirst = false;
                }
                else
                {
                    stream.WriteByte((byte)'\n');
                }

                await item.SerializeAsync(stream, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
