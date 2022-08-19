namespace Sentry.Extensibility
{
    /// <summary>
    /// Process a <see cref="Transaction"/> during the prepare phase.
    /// </summary>
    public interface ISentryTransactionProcessor
    {
        /// <summary>
        /// Process the <see cref="Transaction"/>
        /// </summary>
        /// <param name="transaction">The Transaction to process</param>
        void Process(Transaction transaction){}
    }
}
