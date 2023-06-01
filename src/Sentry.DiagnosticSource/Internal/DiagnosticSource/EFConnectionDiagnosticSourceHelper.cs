using Sentry.Internal.Extensions;

namespace Sentry.Internal.DiagnosticSource;

// ReSharper disable once InconsistentNaming
internal class EFConnectionDiagnosticSourceHelper : EFDiagnosticSourceHelper
{
    internal EFConnectionDiagnosticSourceHelper(IHub hub, SentryOptions options, AsyncLocal<WeakReference<ISpan>> spanLocal, object? diagnosticSourceValue)
        : base(hub, options, diagnosticSourceValue)
    {
    }

    protected override string Operation => "db.connection";
    protected override string Description => null!;

    private Guid? ConnectionId => DiagnosticSourceValue?.GetGuidProperty("ConnectionId");
        // The following would be more restrictive but harder to mock/test
        // DiagnosticSourceValue?.GetType().FullName == "Microsoft.EntityFrameworkCore.Diagnostics.ConnectionEventData"
        //     ? DiagnosticSourceValue?.GetGuidProperty("ConnectionId") :
        //     null;

    private bool CorrelatedSpan(SpanTracer span) =>
        span.TraceData.ContainsKey(nameof(ConnectionId)) &&
        span.TraceData[nameof(ConnectionId)] is Guid traceConnectionId &&
        ConnectionId == traceConnectionId;

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
            spanTracer.TraceData[nameof(ConnectionId)] = ConnectionId;
            return;
        }
        base.SetSpanReference(span);
    }
}
