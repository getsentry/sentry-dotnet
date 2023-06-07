using Sentry.Extensibility;
using Sentry.Internal.Extensions;

namespace Sentry.Internal.DiagnosticSource;

internal class EFCommandDiagnosticSourceHelper : EFDiagnosticSourceHelper
{
    internal EFCommandDiagnosticSourceHelper(IHub hub, SentryOptions options) : base(hub, options)
    {
    }

    protected override string Operation => "db.query";
    protected override string Description(object? diagnosticSourceValue) => FilterNewLineValue(diagnosticSourceValue) ?? string.Empty;
    private static Guid? GetCommandId(object? diagnosticSourceValue) => diagnosticSourceValue?.GetGuidProperty("CommandId");

    private static void SetCommandId(ISpan span, Guid? commandId)
    {
        Debug.Assert(commandId != Guid.Empty);

        span.SetExtra(CommandExtraKey, commandId);
    }

    private static Guid? TryGetCommandId(ISpan span) =>
        span.Extra.TryGetValue(CommandExtraKey, out var key) && key is Guid guid
            ? guid
            : null;

    protected override ISpan GetParentSpan(ITransaction transaction, object? diagnosticSourceValue)
    {
        if (GetConnectionId(diagnosticSourceValue) is { } connectionId)
        {
            var parentSpan = transaction.Spans
                .FirstOrDefault(span =>
                    !span.IsFinished &&
                    span.Operation == "db.connection" &&
                    TryGetConnectionId(span) == connectionId);
            if (parentSpan is { })
            {
                return parentSpan;
            }
            Options.LogWarning("Unable to locate query parent span. No Span found with db connection id {0}.", connectionId);
            return base.GetParentSpan(transaction, diagnosticSourceValue);
        }

        Options.LogWarning("Unable to locate query parent span. diagnostic source did not contain a db connection id.");
        return base.GetParentSpan(transaction, diagnosticSourceValue);
    }


    protected override ISpan? GetSpanReference(ITransaction transaction, object? diagnosticSourceValue)
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
