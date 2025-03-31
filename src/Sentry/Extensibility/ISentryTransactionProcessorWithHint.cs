namespace Sentry.Extensibility;

/// <summary>
/// Process a <see cref="SentryTransaction"/> during the prepare phase.
/// </summary>
public interface ISentryTransactionProcessorWithHint : ISentryTransactionProcessor
{
    /// <summary>
    /// Process the <see cref="SentryTransaction"/>
    /// </summary>
    /// <param name="transaction">The Transaction to process</param>
    /// <param name="hint">A <see cref="SentryHint"/> with context that may be useful prior to sending the transaction</param>
    /// <remarks>
    /// The transaction returned can be the same instance received or a new one.
    /// Returning null will stop the processing pipeline.
    /// Meaning the transaction should no longer be processed nor send.
    /// </remarks>
    public SentryTransaction? Process(SentryTransaction transaction, SentryHint hint);
}
