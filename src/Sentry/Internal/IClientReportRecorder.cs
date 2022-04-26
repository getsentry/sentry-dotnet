using System.Collections.Generic;
using Sentry.Protocol.Envelopes;

namespace Sentry.Internal
{
    internal interface IClientReportRecorder
    {
        void RecordDiscardedEvent(DiscardReason reason, DataCategory category);
        void AttachClientReport(ICollection<EnvelopeItem> envelopeItems, SentryId? eventId);
    }
}
