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

    // protected override void SetSpanReference(ISpan span)
    // {
    //     if (span is SpanTracer spanTracer)
    //     {
    //         spanTracer.TraceData["Connection"] = Description;
    //     }
    // }

    protected override ISpan? GetSpanReference()
    {
        return base.GetSpanReference();
        // Try to return a correlated span if we can find one.
        // if (diagnosticSourceValue is ConnectionEventData connectionEventData &&
        //     transaction.TryGetSpanFromTraceData(s =>
        //         s.TraceData["ConnectionId"] is Guid connectionId
        //         && connectionId == connectionEventData.ConnectionId, out var correlatedSpan)
        //    )
        // {
        //     return correlatedSpan;
        // }

        // If we only have a span for one unfinished db connection we can assume it's the one we want.

        // Otherwise we have no way of knowing which Transaction to return so we'll just return null.
        // This shouldn't ordinarily happen so we'll log a warning.

    }
}
