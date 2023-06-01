using Sentry.Internal.Extensions;

namespace Sentry.Internal.DiagnosticSource;

// ReSharper disable once InconsistentNaming
internal class EFQueryCompilerDiagnosticSourceHelper : EFDiagnosticSourceHelper
{
    internal EFQueryCompilerDiagnosticSourceHelper(IHub hub, SentryOptions options, AsyncLocal<WeakReference<ISpan>> spanLocal, object? diagnosticSourceValue)
        : base(hub, options, diagnosticSourceValue)
    {
    }

    protected override string Operation => "db.query.compile";
    protected override string Description => FilterNewLineValue(DiagnosticSourceValue) ?? string.Empty;

    private string QueryExpression => Description;

    private bool SameExpression(SpanTracer span) =>
        span.TraceData.ContainsKey(nameof(QueryExpression)) &&
        span.TraceData[nameof(QueryExpression)] is string traceQueryExpression &&
        QueryExpression == traceQueryExpression;

    protected override ISpan? GetSpanReference(ITransaction transaction)
    {
        // In the case of Query Compilation events, we don't get any correlation id from the diagnostic data. The best
        // we can do is to use the query expression. This isn't guaranteed to be unique so we grab the first match.
        if (TryGetFirstSpanFromTraceData(transaction, SameExpression, out var looselyCorrelatedSpan))
        {
            return looselyCorrelatedSpan;
        }
        return base.GetSpanReference(transaction);
    }

    protected override void SetSpanReference(ISpan span)
    {
        if (span is SpanTracer spanTracer)
        {
            spanTracer.TraceData[nameof(QueryExpression)] = QueryExpression;
            return;
        }
        base.SetSpanReference(span);
    }
}
