namespace Sentry.Extensibility;

/// <summary>
/// Process a <see cref="Transaction"/> during the prepare phase.
/// </summary>
public interface ISentryTransactionProcessorWithHint: ISentryTransactionProcessor
{
    /// <summary>
    /// Process the <see cref="Transaction"/>
    /// </summary>
    /// <param name="transaction">The Transaction to process</param>
    /// <param name="hint">A <see cref="Hint"/> with context that may be useful prior to sending the transaction</param>
    /// <remarks>
    /// The transaction returned can be the same instance received or a new one.
    /// Returning null will stop the processing pipeline.
    /// Meaning the transaction should no longer be processed nor send.
    /// </remarks>
    Transaction? Process(Transaction transaction, Hint hint);
}
