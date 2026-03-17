using Sentry.Extensibility;
using Sentry.Internal.OpenTelemetry;

namespace Sentry.Internal;

internal class TraceIgnoreStatusCodeTransactionProcessor : ISentryTransactionProcessor
{
    private readonly SentryOptions _options;

    public TraceIgnoreStatusCodeTransactionProcessor(SentryOptions options)
    {
        _options = options;
    }

    public SentryTransaction? Process(SentryTransaction transaction)
    {
        if (_options.TraceIgnoreStatusCodes.Count == 0)
        {
            return transaction;
        }

        if (transaction.Data.TryGetValue(OtelSemanticConventions.AttributeHttpResponseStatusCode, out var statusCodeObj)
            && statusCodeObj is int statusCode
            && _options.TraceIgnoreStatusCodes.ContainsStatusCode(statusCode))
        {
            return null;
        }

        return transaction;
    }
}
