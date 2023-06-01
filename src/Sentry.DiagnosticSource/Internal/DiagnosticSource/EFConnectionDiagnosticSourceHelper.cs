using Sentry.Extensibility;
using Sentry.Internal.Extensions;

namespace Sentry.Internal.DiagnosticSource;

internal class EFConnectionDiagnosticSourceHelper : EFDiagnosticSourceHelper
{
    internal EFConnectionDiagnosticSourceHelper(IHub hub, SentryOptions options) : base(hub, options)
    {
    }

    protected override string Operation => "db.connection";
    protected override string Description(object? diagnosticSourceValue) => null!;
    private static Guid? GetConnectionId(object? diagnosticSourceValue) => diagnosticSourceValue?.GetGuidProperty("ConnectionId");

    private static void SetConnectionId(ISpan span, Guid? connectionId)
    {
        Debug.Assert(connectionId != Guid.Empty);

        span.SetExtra(ConnectionExtraKey, connectionId);
    }

    private static Guid? TryGetConnectionId(ISpan span) =>
        span.Extra.TryGetValue(ConnectionExtraKey, out var key) && key is Guid guid
            ? guid
            : null;

    protected override ISpan? GetSpanReference(ITransaction transaction, object? diagnosticSourceValue)
    {
        if (GetConnectionId(diagnosticSourceValue) is { } connectionId)
        {
            return transaction.Spans
                .FirstOrDefault(span =>
                    !span.IsFinished &&
                    span.Operation == Operation &&
                    TryGetConnectionId(span) == connectionId);
        }
        Options.LogWarning("No correlation id found for {1}.", Operation);
        return null;
    }

    protected override void SetSpanReference(ISpan span, object? diagnosticSourceValue)
    {
        if (GetConnectionId(diagnosticSourceValue) is { } connectionId)
        {
            SetConnectionId(span, connectionId);
            return;
        }
        Options.LogWarning("No {0} found when adding {1} Span.", "ConnectionId", Operation);
    }
}
