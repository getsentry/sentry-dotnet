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
    private Guid? ConnectionId => DiagnosticSourceValue?.GetGuidProperty("ConnectionId");
    private Guid? CommandId => DiagnosticSourceValue?.GetGuidProperty("CommandId");

    private static void SetCommandId(ISpan span, Guid? commandId)
    {
        Debug.Assert(commandId != Guid.Empty);

        span.SetExtra(CommandExtraKey, commandId);
    }

    private static Guid? TryGetCommandId(ISpan span) =>
        span.Extra.TryGetValue(CommandExtraKey, out var key) && key is Guid guid
            ? guid
            : null;

    protected override ISpan? GetSpanReference(ITransaction transaction) =>
        CommandId is { } commandId
            ? transaction.Spans
                .FirstOrDefault(span =>
                    !span.IsFinished &&
                    span.Operation == Operation &&
                    TryGetCommandId(span) == commandId)
            : base.GetSpanReference(transaction);

    protected override void SetSpanReference(ISpan span)
    {
        if (CommandId is { })
        {
            SetCommandId(span, CommandId);
            return;
        }
        base.SetSpanReference(span);
    }
}
