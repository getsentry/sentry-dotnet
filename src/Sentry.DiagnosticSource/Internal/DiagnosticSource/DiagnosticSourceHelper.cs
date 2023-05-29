using Sentry.Extensibility;

namespace Sentry.Internal.DiagnosticSource;

internal abstract class DiagnosticSourceHelper
{
    private SentryOptions Options { get; }
    protected AsyncLocal<WeakReference<ISpan>> SpanLocal { get; }
    protected object? DiagnosticSourceValue { get; }
    private ITransaction? Transaction { get; set; }
    protected abstract string Operation { get; }
    protected abstract string Description { get; }

    internal DiagnosticSourceHelper(IHub hub, SentryOptions options, AsyncLocal<WeakReference<ISpan>> spanLocal, object? diagnosticSourceValue)
    {
        Options = options;
        SpanLocal = spanLocal;
        DiagnosticSourceValue = diagnosticSourceValue;
        Transaction = hub.GetTransactionIfSampled();
    }

    internal void AddSpan()
    {
        Options.LogDebug($"(Sentry add span {Operation})");
        LogTransactionSpans();
        if (Transaction == null)
        {
            return;
        }

        var parent = GetParentSpan(Transaction);
        var child = parent.StartChild(Operation, Description);

        SetSpanReference(child);
    }

    internal void FinishSpan(SpanStatus status)
    {
        if (Transaction == null)
        {
            return;
        }

        Options.LogDebug($"(Sentry finish span {Operation})");
        LogTransactionSpans();

        var sourceSpan = GetSpanReference(Transaction);
        if (sourceSpan == null)
        {
            Options.LogWarning("Trying to close a span that was already garbage collected. {0}", Operation);
            return;
        }

        sourceSpan.Finish(status);
    }

    private void LogTransactionSpans()
    {
        if (Transaction == null)
        {
            Options.LogDebug($"(Sentry transaction is null)");
            return;
        }

        Options.LogDebug("Transaction Spans");
        Options.LogDebug("-----------------");
        foreach (var span in Transaction.Spans)
        {
            Options.LogDebug($"id: {span.SpanId} operation: {span.Operation}");
        }
    }

    /// <summary>
    /// Get the Query with error message and remove the unneeded values.
    /// </summary>
    /// <example>
    /// Compiling query model:
    /// EF initialize...\r\nEF Query...
    /// becomes:
    /// EF Query...
    /// </example>
    /// <param name="value">the query to be parsed value</param>
    /// <returns>the filtered query</returns>
    internal static string? FilterNewLineValue(object? value)
    {
        var str = value?.ToString();
        return str?[(str.IndexOf('\n') + 1)..];
    }

    protected abstract ISpan GetParentSpan(ITransaction transaction);

    protected virtual void SetSpanReference(ISpan span)
    {
        SpanLocal.Value = new WeakReference<ISpan>(span);
    }

    protected virtual ISpan? GetSpanReference(ITransaction transaction)
    {
        return (SpanLocal.Value is { } reference && reference.TryGetTarget(out var startedSpan))
            ? startedSpan
            : null;
    }
}
