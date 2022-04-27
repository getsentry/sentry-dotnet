using System.Collections.Generic;
using System.Linq;
using Sentry.Infrastructure;

namespace Sentry.Internal
{
    internal class ClientReportRecorder : IClientReportRecorder
    {
        private readonly SentryOptions _sentryOptions;
        private readonly ISystemClock _clock;

        private readonly ThreadsafeCounterDictionary<DiscardReasonWithCategory> _discardedEvents = new();

        // discarded events are exposed internally for testing
        internal IReadOnlyDictionary<DiscardReasonWithCategory, int> DiscardedEvents => _discardedEvents;

        public ClientReportRecorder(SentryOptions sentryOptions, ISystemClock? clock = default)
        {
            _sentryOptions = sentryOptions;
            _clock = clock ?? new SystemClock();
        }

        public void RecordDiscardedEvent(DiscardReason reason, DataCategory category)
        {
            // Don't count discarded events if we're not going to be sending them.
            if (!_sentryOptions.SendClientReports)
            {
                return;
            }

            // Increment the counter for the discarded event.
            _discardedEvents.Increment(reason.WithCategory(category));
        }

        public ClientReport? GenerateClientReport()
        {
            // Don't attach a client report if we've turned them off.
            if (!_sentryOptions.SendClientReports)
            {
                return null;
            }

            // Read and reset discarded events.
            var discardedEvents = _discardedEvents.ReadAllAndReset();

            // Don't attach a client report if there's nothing to report.
            if (!discardedEvents.Any(x => x.Value > 0))
            {
                return null;
            }

            // Generate the client report.
            var timestamp = _clock.GetUtcNow();
            return new ClientReport(timestamp, discardedEvents);
        }
    }
}
