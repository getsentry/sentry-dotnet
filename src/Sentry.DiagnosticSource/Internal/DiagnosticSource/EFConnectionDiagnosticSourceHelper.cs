using Sentry.Extensibility;

namespace Sentry.Internal.DiagnosticSource;

internal class EFConnectionDiagnosticSourceHelper : EFDiagnosticSourceHelper
{
    internal EFConnectionDiagnosticSourceHelper(IHub hub, SentryOptions options) : base(hub, options)
    {
    }

    protected override string Operation => "db.connection";

    protected override string? GetDescription(object? diagnosticSourceValue) => GetDatabaseName(diagnosticSourceValue);

    protected override ISpan? GetSpanReference(ITransactionTracer transaction, object? diagnosticSourceValue)
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
        }
        else
        {
            Options.LogWarning("No {0} found when adding {1} Span.", "ConnectionId", Operation);
        }
    }

    /// <summary>
    /// EF Connections are often pooled. If we see the same connection multiple times, we reuse the span so that it
    /// shows as a single connection in the resulting waterfall chart on Sentry.
    /// </summary>
    /// <param name="diagnosticSourceValue"></param>
    internal void AddOrReuseSpan(object? diagnosticSourceValue)
    {
        if (GetConnectionId(diagnosticSourceValue) is { } connectionId)
        {
            Options.LogDebug($"Checking for span to reuse for {Operation} with connection id {connectionId}");
            LogTransactionSpans();
            if (Transaction is { } transaction)
            {
                var spanWithConnectionId = transaction.Spans
                    .FirstOrDefault(span =>
                        span.Operation == Operation &&
                        TryGetConnectionId(span) == connectionId);
                if (spanWithConnectionId is SpanTracer existingSpan)
                {
                    // OK we've seen this connection before... let's reuse it
                    Options.LogDebug($"Reusing span for {Operation} with connection id {connectionId}");
                    existingSpan.Unfinish();
                    return;
                }
            }
        }

        // If we can't find a span to reuse then we'll add a new one instead
        AddSpan(diagnosticSourceValue);
    }
}
