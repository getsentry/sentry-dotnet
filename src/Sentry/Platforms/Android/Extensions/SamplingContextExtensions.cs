using Sentry.Android.Facades;

namespace Sentry.Android.Extensions;

internal static class SamplingContextExtensions
{
    private static readonly Dictionary<string, object?> EmptyObjectDictionary = new();

    public static TransactionSamplingContext ToTransactionSamplingContext(this JavaSdk.SamplingContext context)
    {
        var transactionContext = new TransactionContextFacade(context.TransactionContext);

        //var customSamplingContext = context.CustomSamplingContext?.Data
        //    .ToDictionary(x => x.Key, x => (object?)x.Value) ?? EmptyObjectDictionary;

        var customSamplingContext = ((IReadOnlyDictionary<string, object?>?)context.CustomSamplingContext?.Data)
             ?? EmptyObjectDictionary;

        return new(transactionContext, customSamplingContext);
    }
}
