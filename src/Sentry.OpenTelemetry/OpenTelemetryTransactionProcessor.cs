using Sentry.Extensibility;
using Sentry.Internal.Tracing;

namespace Sentry.OpenTelemetry;

internal class OpenTelemetryTransactionProcessor : ISentryTransactionProcessor
{
    public SentryTransaction Process(SentryTransaction transaction)
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
