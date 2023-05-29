namespace Sentry.Internal.DiagnosticSource;

internal class CommandDiagnosticSourceHelper : DiagnosticSourceHelper
{
    internal CommandDiagnosticSourceHelper(IHub hub, SentryOptions options, AsyncLocal<WeakReference<ISpan>> spanLocal, object? diagnosticSourceValue)
        : base(hub, options, spanLocal, diagnosticSourceValue)
    {
    }

    protected override string Operation => "db.query";
    protected override string Description => FilterNewLineValue(DiagnosticSourceValue) ?? string.Empty;

    protected override ISpan GetParentSpan(ITransaction transaction)
        => transaction.GetLastActiveSpan() ?? transaction.GetDbParentSpan();

    // protected override void SetSpanReference(ISpan span)
    // {
    //     if (span is SpanTracer spanTracer)
    //     {
    //         spanTracer.TraceData["Query"] = Description;
    //     }
    // }

    protected override ISpan? GetSpanReference()
    {
        return base.GetSpanReference();
        // Try to return a correlated span if we can find one.
        // if (diagnosticSourceValue is CommandEventData connectionEventData &&
        //     transaction.TryGetSpanFromTraceData(s =>
        //         s.TraceData["CommandId"] is Guid commandId
        //         && commandId == connectionEventData.CommandId, out var correlatedSpan)
        //    )
        // {
        //     return correlatedSpan;
        // }

        // If we only have a span for one unfinished query then we can assume it's the one we want.

        // Otherwise we have no way of knowing which Transaction to return so we'll just return null.
        // This shouldn't ordinarily happen so we'll log a warning.
    }
}
