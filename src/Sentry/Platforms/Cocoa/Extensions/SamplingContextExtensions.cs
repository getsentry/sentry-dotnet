using Sentry.Cocoa.Facades;

namespace Sentry.Cocoa.Extensions;

internal static class SamplingContextExtensions
{
    public static TransactionSamplingContext ToTransactionSamplingContext(this CocoaSdk.SentrySamplingContext context)
    {
        var transactionContext = new TransactionContextFacade(context.TransactionContext);
        var customSamplingContext = context.CustomSamplingContext.ToObjectDictionary();
        return new TransactionSamplingContext(transactionContext, customSamplingContext);
    }
}
