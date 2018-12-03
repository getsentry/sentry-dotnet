namespace Sentry
{
    /// <summary>
    /// Sentry Client interface
    /// </summary>
    public interface ISentryClient
    {
        /// <summary>
        /// Whether the client is enabled or not
        /// </summary>
        bool IsEnabled { get; }

        /// <summary>
        /// Capture the event
        /// </summary>
        /// <param name="evt">The event to be captured</param>
        /// <param name="scope">An optional scope to be applied to the event.</param>
        /// <returns>The Id of the event</returns>
        SentryId CaptureEvent(SentryEvent evt, Scope scope = null);
    }
}
