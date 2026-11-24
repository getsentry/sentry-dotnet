namespace Sentry.Internal;

internal static class TransactionExtensions
{
    public static DynamicSamplingContext? GetDynamicSamplingContext(this ITransactionTracer transaction)
    {
        if (transaction is UnsampledTransaction unsampledTransaction)
        {
            return unsampledTransaction.DynamicSamplingContext;
        }
        if (transaction is TransactionTracer transactionTracer)
        {
            return transactionTracer.DynamicSamplingContext;
        }
        return null;
    }

    public static void SetDynamicSamplingContext(this ITransactionTracer transaction, DynamicSamplingContext dsc)
    {
        if (transaction is UnsampledTransaction unsampledTransaction)
        {
            unsampledTransaction.DynamicSamplingContext = dsc;
        }
        else if (transaction is TransactionTracer transactionTracer)
        {
            transactionTracer.DynamicSamplingContext = dsc;
        }
    }
}
