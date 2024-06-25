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

    private Guid? GetCommandId(object? diagnosticSourceValue) => diagnosticSourceValue?.GetGuidProperty("CommandId", Options.DiagnosticLogger);

    private static void SetCommandId(ISpan span, Guid? commandId)
    {
        Debug.Assert(commandId != Guid.Empty);

        span.SetExtra(EFKeys.DbCommandId, commandId);
    }

    private static Guid? TryGetCommandId(ISpan span) => span.Extra.TryGetValue<string, Guid?>(EFKeys.DbCommandId);

    protected override ISpan? GetSpanReference(ITransactionTracer transaction, object? diagnosticSourceValue)
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

    protected override void SetSpanReference(ISpan span, object? diagnosticSourceValue)
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
