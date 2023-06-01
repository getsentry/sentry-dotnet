using Sentry.Internal.Extensions;

namespace Sentry.Internal.DiagnosticSource;

// ReSharper disable once InconsistentNaming
internal class EFCommandDiagnosticSourceHelper : EFDiagnosticSourceHelper
{
    internal EFCommandDiagnosticSourceHelper(IHub hub, SentryOptions options, AsyncLocal<WeakReference<ISpan>> spanLocal, object? diagnosticSourceValue)
        : base(hub, options, diagnosticSourceValue)
    {
    }

    protected override string Operation => "db.query";
    protected override string Description => FilterNewLineValue(DiagnosticSourceValue) ?? string.Empty;

    private Guid? CommandId => DiagnosticSourceValue?.GetGuidProperty("CommandId");
        // The following would be more restrictive but harder to mock/test
        // DiagnosticSourceValue?.GetType().FullName == "Microsoft.EntityFrameworkCore.Diagnostics.CommandEventData"
        //     ? DiagnosticSourceValue?.GetGuidProperty("CommandId") :
        //     null;

    private bool CorrelatedSpan(SpanTracer span) =>
        span.TraceData.ContainsKey(nameof(CommandId)) &&
        span.TraceData[nameof(CommandId)] is Guid traceCommandId &&
        CommandId == traceCommandId;

    protected override ISpan? GetSpanReference(ITransaction transaction)
    {
        // Try to return a correlated span if we can find one.
        if (TryGetSpanFromTraceData(transaction, CorrelatedSpan, out var correlatedSpan))
        {
            return correlatedSpan;
        }
        return base.GetSpanReference(transaction);
    }

    protected override void SetSpanReference(ISpan span)
    {
        if (span is SpanTracer spanTracer)
        {
            spanTracer.TraceData[nameof(CommandId)] = CommandId;
            return;
        }
        base.SetSpanReference(span);
    }
}
