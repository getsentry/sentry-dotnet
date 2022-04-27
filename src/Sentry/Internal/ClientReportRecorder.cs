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
            _discardedEvents.Increment(reason.WithCategory(category));
        }

        public ClientReport? GenerateClientReport()
        {
            // Read and reset discarded events even if we're not sending them, to prevent excessive growth over time.
            var discardedEvents = _discardedEvents.ReadAllAndReset();

            // Don't attach a client report if we've turned them off or if there's nothing to report.
            if (!_sentryOptions.SendClientReports || !discardedEvents.Any(x => x.Value > 0))
            {
                return null;
            }

            // Generate the client report.
            var timestamp = _clock.GetUtcNow();
            return new ClientReport(timestamp, discardedEvents);
        }
    }
}
