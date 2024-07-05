using Sentry.Protocol.Envelopes;

namespace Sentry.Internal.Extensions;

internal static class ClientReportExtensions
{
    public static void RecordDiscardedEvents(this IClientReportRecorder recorder, DiscardReason reason, Envelope envelope)
    {
        foreach (var item in envelope.Items)
        {
            recorder.RecordDiscardedEvent(reason, item.DataCategory);
            if (item.DataCategory.Equals(DataCategory.Transaction))
            {
                if (item.Payload is JsonSerializable { Source: SentryTransaction transaction })
                {
                    recorder.RecordDiscardedEvent(reason, DataCategory.Span, transaction.Spans.Count + 1);
                }
            }
        }
    }
}
