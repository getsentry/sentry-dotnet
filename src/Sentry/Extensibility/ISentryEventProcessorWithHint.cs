namespace Sentry.Extensibility;

/// <summary>
/// Process a SentryEvent during the prepare phase.
/// </summary>
public interface ISentryEventProcessorWithHint : ISentryEventProcessor
{
    /// <summary>
    /// Process the <see cref="SentryEvent"/>
    /// </summary>
    /// <param name="event">The event to process</param>
    /// <param name="hint">A <see cref="SentryHint"/> with context that may be useful prior to sending the event</param>
    /// <return>The processed event or <c>null</c> if the event was dropped.</return>
    /// <remarks>
    /// The event returned can be the same instance received or a new one.
    /// Returning null will stop the processing pipeline so that the event will neither be processed by
    /// additional event processors or sent to Sentry.
    /// </remarks>
    public SentryEvent? Process(SentryEvent @event, SentryHint hint);
}

