namespace Sentry.Internal;

/// <summary>
/// Factory to create/attach profilers when a transaction starts.
/// </summary>
internal interface ITransactionProfilerFactory
{
    /// <summary>
    /// Called during transaction start to start a new profiler, if applicable.
    /// </summary>
    ITransactionProfiler? Start(ITransactionTracer transaction, CancellationToken cancellationToken);
}

/// <summary>
/// A profiler collecting ProfileInfo for a given transaction.
/// </summary>
internal interface ITransactionProfiler
{
    /// <summary>
    /// Called when the transaction ends - this should stop profile samples collection.
    /// </summary>
    void Finish();

    /// <summary>Process and collect the profile.</summary>
    /// <returns>The collected profile.</returns>
    ISerializable? Collect(SentryTransaction transaction);
}
