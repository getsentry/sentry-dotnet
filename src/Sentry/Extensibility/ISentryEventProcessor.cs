namespace Sentry.Extensibility;

/// <summary>
/// Process a SentryEvent during the prepare phase.
/// </summary>
public interface ISentryEventProcessor
{
    /// <summary>
    /// Process the <see cref="SentryEvent"/>
    /// </summary>
    /// <param name="event">The event to process</param>
    /// <return>The processed event or <c>null</c> if the event was dropped.</return>
    /// <remarks>
    /// The event returned can be the same instance received or a new one.
    /// Returning null will stop the processing pipeline.
    /// Meaning the event should no longer be processed nor send.
    /// </remarks>
    public SentryEvent? Process(SentryEvent @event);
}

internal static class ISentryEventProcessorExtensions
{
    internal static SentryEvent? DoProcessEvent(this ISentryEventProcessor processor, SentryEvent @event, SentryHint hint)
    {
        return (processor is ISentryEventProcessorWithHint contextualProcessor)
            ? contextualProcessor.Process(@event, hint)
            : processor.Process(@event);
    }
}
