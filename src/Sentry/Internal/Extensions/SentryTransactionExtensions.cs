using Sentry.Protocol;

namespace Sentry.Internal.Extensions;

internal static class SentryTransactionExtensions
{
    /// <summary>
    /// Allows us to convert a Transaction and its chiled spans to the new SpanV2 format during the transition period.
    /// This is temporary - we can remove it once transactions have been deprecated.
    /// </summary>
    public static IEnumerable<SpanV2> ToSpanV2Spans(this SentryTransaction transaction)
    {
        // Collect spans: transaction span + child spans.
        yield return new SpanV2(transaction);
        foreach (var span in transaction.Spans)
        {
            yield return new SpanV2(span);
        }
    }
}
