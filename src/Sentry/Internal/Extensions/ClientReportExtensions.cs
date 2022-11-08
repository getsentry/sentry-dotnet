using Sentry.Protocol.Envelopes;

namespace Sentry.Internal.Extensions;

internal static class ClientReportExtensions
{
    public static void RecordDiscardedEvents(this IClientReportRecorder recorder, DiscardReason reason, Envelope envelope)
    {
        foreach (var item in envelope.Items)
        {
            recorder.RecordDiscardedEvent(reason, item.DataCategory);
        }
    }
}
