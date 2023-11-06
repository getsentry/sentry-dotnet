using Sentry.Extensibility;
using Sentry.Internal.Extensions;

namespace Sentry.Internal.DiagnosticSource;

internal class EFCommandDiagnosticSourceHelper : EFDiagnosticSourceHelper
{
    internal EFCommandDiagnosticSourceHelper(IHub hub, SentryOptions options) : base(hub, options)
    {
    }

    protected override string Operation => "db.query";

    protected override string GetDescription(object? diagnosticSourceValue) => FilterNewLineValue(diagnosticSourceValue) ?? string.Empty;

    private static Guid? GetCommandId(object? diagnosticSourceValue) => diagnosticSourceValue?.GetGuidProperty("CommandId");

    private static void SetCommandId(ISpanTracer span, Guid? commandId)
    {
        Debug.Assert(commandId != Guid.Empty);

        span.Extra[EFKeys.DbCommandId] = commandId;
    }

    private static Guid? TryGetCommandId(ISpanTracer span) => span.Extra.TryGetValue<string, Guid?>(EFKeys.DbCommandId);

    protected override ISpanTracer? GetSpanReference(ITransactionTracer transaction, object? diagnosticSourceValue)
    {
        if (GetCommandId(diagnosticSourceValue) is { } commandId)
        {
            return transaction.Spans
                .FirstOrDefault(span =>
                    !span.IsFinished &&
                    span.Operation == Operation &&
                    TryGetCommandId(span) == commandId);
        }
        Options.LogWarning("No correlation id found for {1}.", Operation);
        return null;
    }

    protected override void SetSpanReference(ISpanTracer span, object? diagnosticSourceValue)
    {
        if (GetCommandId(diagnosticSourceValue) is { } commandId)
        {
            SetCommandId(span, commandId);
            if (GetConnectionId(diagnosticSourceValue) is { } connectionId)
            {
                SetConnectionId(span, connectionId);
            }
            return;
        }
        Options.LogWarning("No correlation id can be set for {1}.", Operation);
    }
}
