using Sentry.Android.Facades;

namespace Sentry.Android.Extensions;

internal static class SamplingContextExtensions
{
    public static TransactionSamplingContext ToTransactionSamplingContext(this Java.SamplingContext context)
    {
        var transactionContext = new TransactionContextFacade(context.TransactionContext);

        var customSamplingContext = context.CustomSamplingContext?
            .Data.ToDictionary(x => x.Key, x => (object?)x.Value)
            ?? new Dictionary<string, object?>();

        return new(transactionContext, customSamplingContext);
    }
}
