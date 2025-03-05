namespace Sentry.Internal;

internal interface IClientReportRecorder
{
    /// <summary>
    /// Records one count of a discarded event, with the given <paramref name="reason"/> and <paramref name="category"/>.
    /// </summary>
    /// <param name="reason">The reason for the event being discarded.</param>
    /// <param name="category">The data category of the event being discardedd.</param>
    /// <param name="quantity">The number of items discarded (defaults to 1)</param>
    public void RecordDiscardedEvent(DiscardReason reason, DataCategory category, int quantity = 1);

    /// <summary>
    /// Generates a <see cref="ClientReport"/> containing counts of discarded events that have been recorded.
    /// Also resets those counts to zero at the same time the report is generated.
    /// </summary>
    /// <returns>
    /// The <see cref="ClientReport"/>, as long as there is something to report.
    /// Returns <c>null</c> if there were no discarded events recorded since the previous call to this method.
    /// </returns>
    public ClientReport? GenerateClientReport();

    /// <summary>
    /// Loads the current instance with the events from the provided <paramref name="report"/>.
    /// </summary>
    /// <remarks>
    /// Useful when recovering from failures while sending client reports.
    /// </remarks>
    /// <param name="report">The client report to load.</param>
    public void Load(ClientReport report);
}
