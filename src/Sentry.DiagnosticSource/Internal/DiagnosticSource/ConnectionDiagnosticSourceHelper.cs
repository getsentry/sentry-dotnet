#if NETSTANDARD2_1_OR_GREATER
using Microsoft.EntityFrameworkCore.Diagnostics;
#endif

namespace Sentry.Internal.DiagnosticSource;

internal class ConnectionDiagnosticSourceHelper : DiagnosticSourceHelper
{
    internal ConnectionDiagnosticSourceHelper(IHub hub, SentryOptions options, AsyncLocal<WeakReference<ISpan>> spanLocal, object? diagnosticSourceValue)
        : base(hub, options, spanLocal, diagnosticSourceValue)
    {
    }

    protected override string Operation => "db.connection";
    protected override string Description => null!;

    protected override ISpan GetParentSpan(ITransaction transaction) => transaction.GetDbParentSpan();

#if NETSTANDARD2_1_OR_GREATER
    protected override void SetSpanReference(ISpan span)
    {
        if (span is SpanTracer spanTracer && DiagnosticSourceValue is ConnectionEventData connectionEventData)
        {
            spanTracer.TraceData["ConnectionId"] = connectionEventData.ConnectionId;
            return;
        }
    }

    protected override ISpan? GetSpanReference(ITransaction transaction)
    {
        // Try to return a correlated span if we can find one.
        if (DiagnosticSourceValue is ConnectionEventData connectionEventData &&
            transaction.TryGetSpanFromTraceData(s =>
                s.TraceData["ConnectionId"] is Guid connectionId
                && connectionId == connectionEventData.ConnectionId, out var correlatedSpan)
           )
        {
            return correlatedSpan;
        }

        // If we only have a span for one unfinished db connection we can assume it's the one we want.
        try
        {
            return transaction.Spans
                .OrderByDescending(x => x.StartTimestamp)
                .SingleOrDefault(s => !s.IsFinished && s.Operation.Equals(Operation));
        }
        catch (InvalidOperationException)
        {
            // This exception is thrown if SingleOrDefault matches more than one element
        }

        // Otherwise we have no way of knowing which Transaction to return so we'll just return null.
        // TODO: This shouldn't ordinarily happen so we'll log a warning.
        return null;
    }
#endif
}
