using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        public async Task SerializeAsync(StreamWriter writer, CancellationToken cancellationToken = default)
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
                    await writer.WriteAsync('\n').ConfigureAwait(false);
                }

                await item.SerializeAsync(writer, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
