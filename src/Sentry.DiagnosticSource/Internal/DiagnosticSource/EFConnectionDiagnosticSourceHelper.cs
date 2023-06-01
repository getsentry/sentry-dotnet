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

    private static void SetConnectionId(ISpan span, Guid? connectionId)
    {
        Debug.Assert(connectionId != Guid.Empty);

        span.SetExtra(ConnectionExtraKey, connectionId);
    }

    private static Guid? TryGetConnectionId(ISpan span) =>
        span.Extra.TryGetValue(ConnectionExtraKey, out var key) && key is Guid guid
            ? guid
            : null;

    protected override ISpan? GetSpanReference(ITransaction transaction) =>
        ConnectionId is { } connectionId
            ? transaction.Spans
                .FirstOrDefault(span =>
                    !span.IsFinished &&
                    span.Operation == Operation &&
                    TryGetConnectionId(span) == connectionId)
            : base.GetSpanReference(transaction);

    protected override void SetSpanReference(ISpan span)
    {
        if (ConnectionId is { } connectionId)
        {
            SetConnectionId(span, connectionId);
            return;
        }
        base.SetSpanReference(span);
    }
}
