namespace Sentry.Internal.DiagnosticSource;

internal class QueryCompilerDiagnosticSourceHelper : DiagnosticSourceHelper
{
    internal QueryCompilerDiagnosticSourceHelper(IHub hub, SentryOptions options, AsyncLocal<WeakReference<ISpan>> spanLocal, object? diagnosticSourceValue)
        : base(hub, options, spanLocal, diagnosticSourceValue)
    {
    }

    protected override string Operation => "db.query.compile";
    protected override string Description => FilterNewLineValue(DiagnosticSourceValue) ?? string.Empty;

    protected override ISpan GetParentSpan(ITransaction transaction) => transaction.GetDbParentSpan();

    // protected override void SetSpanReference(ISpan span)
    // {
    //     if (span is SpanTracer spanTracer)
    //     {
    //         spanTracer.TraceData["Query"] = Description;
    //     }
    // }
}
