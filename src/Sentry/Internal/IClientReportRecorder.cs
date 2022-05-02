namespace Sentry.Internal
{
    internal interface IClientReportRecorder
    {
        /// <summary>
        /// Records one count of a discarded event, with the given reason and category.
        /// </summary>
        /// <param name="reason">The reason for the event being discarded.</param>
        /// <param name="category">The data category of the event being discarded.</param>
        void RecordDiscardedEvent(DiscardReason reason, DataCategory category);

        /// <summary>
        /// Generates a client report containing counts of discarded events that have been recorded.
        /// Also resets those counts to zero at the same time the report is generated.
        /// </summary>
        /// <returns>
        /// The client report, as long as there is something to report.
        /// Returns <c>null</c> if there were no discarded events recorded since the previous call to this method.
        /// </returns>
        ClientReport? GenerateClientReport();

        /// <summary>
        /// Loads the recorder with the events from the provided client report.
        /// </summary>
        /// <remarks>
        /// Useful when recovering from failures while sending client reports.
        /// </remarks>
        /// <param name="report">The client report to load into the recorder.</param>
        void Load(ClientReport report);
    }
}
