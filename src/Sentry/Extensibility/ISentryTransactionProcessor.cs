namespace Sentry.Extensibility;

/// <summary>
/// Process a <see cref="SentryTransaction"/> during the prepare phase.
/// </summary>
public interface ISentryTransactionProcessor
{
    /// <summary>
    /// Process the <see cref="SentryTransaction"/>
    /// </summary>
    /// <param name="transaction">The Transaction to process</param>
    /// <remarks>
    /// The transaction returned can be the same instance received or a new one.
    /// Returning null will stop the processing pipeline.
    /// Meaning the transaction should no longer be processed nor send.
    /// </remarks>
    public SentryTransaction? Process(SentryTransaction transaction);
}

internal static class ISentryTransactionProcessorExtensions
{
    internal static SentryTransaction? DoProcessTransaction(this ISentryTransactionProcessor processor, SentryTransaction transaction, SentryHint hint)
    {
        return (processor is ISentryTransactionProcessorWithHint contextualProcessor)
            ? contextualProcessor.Process(transaction, hint)
            : processor.Process(transaction);
    }
}
