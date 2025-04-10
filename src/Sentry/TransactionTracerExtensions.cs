namespace Sentry;


/// <summary>
/// Extensions for ITransactionTracer
/// </summary>
public static class TransactionTracerExtensions
{
    /// <summary>
    /// Waits for the last span to finish
    /// </summary>
    /// <param name="transaction"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static async ValueTask FinishWithLastSpanAsync(this ITransactionTracer transaction, CancellationToken cancellationToken = default)
    {
        if (transaction.IsAllSpansFinished())
        {
            var span = transaction.GetLastFinishedSpan();
            if (span != null)
                transaction.Finish(span.EndTimestamp);
        }
        else
        {
            var span = await transaction.GetLastSpanWhenFinishedAsync(cancellationToken).ConfigureAwait(false);
            if (span != null)
                transaction.Finish(span.EndTimestamp);
        }
    }

    /// <summary>
    /// Checks if all spans are finished within a trnasaction
    /// </summary>
    /// <param name="transaction"></param>
    /// <returns></returns>
    public static bool IsAllSpansFinished(this ITransactionTracer transaction)
        => transaction.Spans.All(x => x.IsFinished);


    /// <summary>
    /// Sorts all spans within a transaction by on end timestamp and returns the last span
    /// </summary>
    /// <param name="transaction"></param>
    /// <returns></returns>
    public static ISpan? GetLastFinishedSpan(this ITransactionTracer transaction)
        => transaction.Spans
            .ToList()
            .Where(x => x.IsFinished)
            .OrderByDescending(x => x.EndTimestamp)
            .LastOrDefault(x => x.IsFinished);

    /// <summary>
    /// Gets the last span (if one), when all spans mark themselves as IsFinished: true
    /// </summary>
    /// <param name="transaction"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static async Task<ISpan?> GetLastSpanWhenFinishedAsync(this ITransactionTracer transaction, CancellationToken cancellationToken = default)
    {
        // what if no spans
        if (transaction.IsAllSpansFinished())
            return transaction.GetLastFinishedSpan();

        var tcs = new TaskCompletionSource<ISpan?>();
        var handler = new EventHandler<SpanStatus?>((_, _) =>
        {
            if (transaction.IsAllSpansFinished())
            {
                var lastSpan = transaction.GetLastFinishedSpan();
                tcs.SetResult(lastSpan);
            }
        });

        try
        {
            foreach (var span in transaction.Spans)
            {
                if (!span.IsFinished)
                {
                    span.StatusChanged += handler;
                }
            }

            return await tcs.Task.ConfigureAwait(false);
        }
        finally
        {
            foreach (var span in transaction.Spans)
            {
                span.StatusChanged -= handler;
            }
        }
    }
}
