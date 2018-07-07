namespace Sentry.Extensibility
{
    /// <summary>
    /// Process a SentryEvent during the prepare phase.
    /// </summary>
    public interface ISentryEventProcessor
    {
        /// <summary>
        /// Process the <see cref="SentryEvent"/>
        /// </summary>
        /// <param name="event">The event to process</param>
        void Process(SentryEvent @event);
    }
}
