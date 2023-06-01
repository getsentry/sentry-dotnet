using Sentry.Extensibility;

namespace Sentry.Internal.DiagnosticSource;

internal abstract class EFDiagnosticSourceHelper
{
    internal const string ConnectionExtraKey = "db.connection_id";
    internal const string CommandExtraKey = "db.command_id";

    protected SentryOptions Options { get; }
    //protected object? DiagnosticSourceValue { get; }
    private ITransaction? Transaction { get; }
    protected abstract string Operation { get; }
    protected abstract string Description(object? diagnosticSourceValue);

    internal EFDiagnosticSourceHelper(IHub hub, SentryOptions options)
    {
        Options = options;
        Transaction = hub.GetTransactionIfSampled();
    }

    internal void AddSpan(object? diagnosticSourceValue)
    {
        Options.LogDebug($"(Sentry add span {Operation})");
        LogTransactionSpans();
        if (Transaction == null)
        {
            return;
        }

        // We "flatten" the EF spans so that they all have the same parent span, for two reasons:
        // 1. Each command typically gets it's own connection, which makes the resulting waterfall diagram hard to read.
        // 2. Sentry's performance errors functionality only works when all queries have the same parent span.
        var parent = Transaction.GetDbParentSpan();
        var child = parent.StartChild(Operation, Description(diagnosticSourceValue));

        SetSpanReference(child, diagnosticSourceValue);
    }

    internal void FinishSpan(object? diagnosticSourceValue, SpanStatus status)
    {
        if (Transaction == null)
        {
            return;
        }

        Options.LogDebug($"(Sentry finish span {Operation})");
        LogTransactionSpans();

        var sourceSpan = GetSpanReference(Transaction, diagnosticSourceValue);
        if (sourceSpan == null)
        {
            Options.LogWarning("Tried to close {0} span but no matching span could be found.", Operation);
            return;
        }
        sourceSpan?.Finish(status);
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

    protected abstract void SetSpanReference(ISpan span, object? diagnosticSourceValue);

    protected abstract ISpan? GetSpanReference(ITransaction transaction, object? diagnosticSourceValue);
}
