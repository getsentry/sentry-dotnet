using Sentry.Extensibility;
using Sentry.Protocol.Envelopes;

namespace Sentry.Internal.Extensions
{
    internal static class TransportExtensions
    {
        internal static void IncrementDiscardedEventCounts(this ITransport transport,
            DiscardReason reason,
            Envelope envelope)
        {
            if (transport is not IDiscardedEventCounter counter)
            {
                return;
            }

            foreach (var item in envelope.Items)
            {
                counter.IncrementCounter(reason, item.DataCategory);
            }
        }

        internal static void IncrementDiscardedEventCounts(this ITransport transport,
            DiscardReason reason,
            DataCategory category)
        {
            if (transport is not IDiscardedEventCounter counter)
            {
                return;
            }

            counter.IncrementCounter(reason, category);
        }
    }
}
