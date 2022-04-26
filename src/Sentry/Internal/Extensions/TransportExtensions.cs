using Sentry.Extensibility;
using Sentry.Protocol.Envelopes;

namespace Sentry.Internal.Extensions
{
    internal static class TransportExtensions
    {
        public static IClientReportRecorder? GetClientReportRecorder(this ITransport transport) =>
            transport is IHasClientReportRecorder transportWithRecorder
                ? transportWithRecorder.ClientReportRecorder
                : null;

        public static void RecordDiscardedEvent(this ITransport transport, DiscardReason reason, DataCategory category) =>
            transport.GetClientReportRecorder()?.RecordDiscardedEvent(reason, category);

        public static void RecordDiscardedEvents(this ITransport transport, DiscardReason reason, Envelope envelope) =>
            transport.GetClientReportRecorder()?.RecordDiscardedEvents(reason, envelope);
    }
}
