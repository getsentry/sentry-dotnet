#if NETSTANDARD2_1_OR_GREATER
using Microsoft.EntityFrameworkCore.Diagnostics;
#endif

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
    {
#if NETSTANDARD2_1_OR_GREATER
        // We can use the ConnectionId to find the connection this command is associated with.
        if (DiagnosticSourceValue is CommandEventData connectionEventData &&
            transaction.TryGetSpanFromTraceData(s =>
                s.TraceData["ConnectionId"] is Guid connectionId
                && connectionId == connectionEventData.ConnectionId, out var correlatedSpan)
           )
        {
            if (correlatedSpan is not null)
            {
                return correlatedSpan;
            }
        }
        // TODO: Log warning... this shouldn't happen
#endif
        return transaction.GetLastActiveSpan() ?? transaction.GetDbParentSpan();
    }

#if NETSTANDARD2_1_OR_GREATER

    protected override void SetSpanReference(ISpan span)
    {
        if (span is SpanTracer spanTracer && DiagnosticSourceValue is CommandEventData commandEventData)
        {
            spanTracer.TraceData["ConnectionId"] = commandEventData.ConnectionId;
            spanTracer.TraceData["CommandId"] = commandEventData.CommandId;
            return;
        }

        base.SetSpanReference(span);
    }

    protected override ISpan? GetSpanReference(ITransaction transaction)
    {
        // Try to return a correlated span if we can find one.
        if (DiagnosticSourceValue is CommandEventData connectionEventData &&
            transaction.TryGetSpanFromTraceData(s =>
                s.TraceData["CommandId"] is Guid commandId
                && commandId == connectionEventData.CommandId, out var correlatedSpan)
           )
        {
            return correlatedSpan;
        }

        // If we only have a span for one unfinished query then we can assume it's the one we want.
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
