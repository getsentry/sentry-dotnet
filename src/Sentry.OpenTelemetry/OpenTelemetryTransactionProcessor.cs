using Sentry.Extensibility;

namespace Sentry.OpenTelemetry;

internal class OpenTelemetryTransactionProcessor : ISentryTransactionProcessor
{
    public Transaction Process(Transaction transaction)
    {
        var activity = Activity.Current;
        if (activity != null)
        {
            var trace = transaction.Contexts.Trace;
            trace.TraceId = activity.TraceId.AsSentryId();
            trace.SpanId = activity.SpanId.AsSentrySpanId();
        }

        return transaction;
    }
}
