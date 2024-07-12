using Sentry.Infrastructure;

namespace Sentry.Internal;

internal class ClientReportRecorder : IClientReportRecorder
{
    private readonly SentryOptions _options;
    private readonly ISystemClock _clock;

    private readonly ThreadsafeCounterDictionary<DiscardReasonWithCategory> _discardedEvents = new();

    // discarded events are exposed internally for testing
    internal IReadOnlyDictionary<DiscardReasonWithCategory, int> DiscardedEvents => _discardedEvents;

    public ClientReportRecorder(SentryOptions options, ISystemClock? clock = default)
    {
        _options = options;
        _clock = clock ?? SystemClock.Clock;
    }

    public void RecordDiscardedEvent(DiscardReason reason, DataCategory category, int quantity = 1)
    {
        // Don't count discarded events if we're not going to be sending them.
        if (!_options.SendClientReports)
        {
            return;
        }

        // Increment the counter for the discarded event.
        _discardedEvents.Add(reason.WithCategory(category), quantity);
    }

    public ClientReport? GenerateClientReport()
    {
        // Don't attach a client report if we've turned them off.
        if (!_options.SendClientReports)
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

    public void Load(ClientReport clientReport)
    {
        foreach (var kvp in clientReport.DiscardedEvents)
        {
            _discardedEvents.Add(kvp.Key, kvp.Value);
        }
    }
}
