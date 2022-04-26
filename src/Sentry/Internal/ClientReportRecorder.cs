using System.Collections.Generic;
using System.Linq;
using Sentry.Extensibility;
using Sentry.Infrastructure;
using Sentry.Protocol.Envelopes;

namespace Sentry.Internal
{
    internal class ClientReportRecorder : IClientReportRecorder
    {
        private readonly SentryOptions _sentryOptions;
        private readonly ISystemClock _clock;

        private readonly ThreadsafeCounterDictionary<DiscardReasonWithCategory> _discardedEvents = new();

        public ClientReportRecorder(SentryOptions sentryOptions, ISystemClock clock)
        {
            _sentryOptions = sentryOptions;
            _clock = clock;
        }

        public void RecordDiscardedEvent(DiscardReason reason, DataCategory category)
        {
            _discardedEvents.Increment(reason.WithCategory(category));
        }

        public void AttachClientReport(ICollection<EnvelopeItem> envelopeItems, SentryId? eventId)
        {
            // Read and reset discards even if we're not sending them (to prevent excessive growth over time)
            var discardedEvents = _discardedEvents.ReadAllAndReset();

            // Don't attach a client report if we've turned them off or if there's nothing to report
            if (!_sentryOptions.SendClientReports || !discardedEvents.Any(x => x.Value > 0))
            {
                return;
            }

            // Create and attach the client report
            var timestamp = _clock.GetUtcNow();
            var clientReport = new ClientReport(timestamp, discardedEvents);
            envelopeItems.Add(EnvelopeItem.FromClientReport(clientReport));
            _sentryOptions.LogDebug("Attached client report to envelope {0}.", eventId);
        }
    }
}
