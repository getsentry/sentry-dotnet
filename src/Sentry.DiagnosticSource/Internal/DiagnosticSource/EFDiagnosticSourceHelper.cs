using Sentry.Extensibility;

namespace Sentry.Internal.DiagnosticSource;

// ReSharper disable once InconsistentNaming
internal abstract class EFDiagnosticSourceHelper
{
    private SentryOptions Options { get; }
    protected object? DiagnosticSourceValue { get; }
    private ITransaction? Transaction { get; set; }
    protected abstract string Operation { get; }
    protected abstract string Description { get; }

    internal EFDiagnosticSourceHelper(IHub hub, SentryOptions options, object? diagnosticSourceValue)
    {
        Options = options;
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

    /// <summary>
    /// Finds an appropriate parent for EF diagnostic spans. Note that in our implementaion we're "flattening" these.
    /// Spans from <see cref="EFQueryCompilerDiagnosticSourceHelper"/> and <see cref="EFCommandDiagnosticSourceHelper"/>
    /// will both have the same parent as those of <see cref="EFConnectionDiagnosticSourceHelper"/>.
    ///
    /// We've done this for two reasons:
    /// 1. We could show these underneath the relevant connection, but each command often gets it's own connection which
    ///    makes the resulting waterfall diagram hard to read.
    /// 2. Sentry has a performance to errors feature which detects n + 1 problems on OM frameworks... but this only
    ///    works if all of the "n" queries have the same parent span in the transaction. Again, since queries ofen run
    ///    on their own connection, if we wrap each query in a connection span, it breaks the performance error handling
    /// </summary>
    private ISpan GetParentSpan(ITransaction transaction) => transaction.GetDbParentSpan();

    protected virtual void SetSpanReference(ISpan span)
    {
        Options.LogDebug($"No Span reference found when adding {Operation} Span.");
    }

    protected virtual ISpan? GetSpanReference(ITransaction transaction)
    {
        Options.LogDebug($"No Span reference found when getting {Operation}. Taking the first unfinished span instead.");
        return transaction.Spans
            .OrderByDescending(x => x.StartTimestamp)
            .FirstOrDefault(s => !s.IsFinished && s.Operation.Equals(Operation));
    }

    protected static bool TryGetSpanFromTraceData(ITransaction transaction, Func<SpanTracer, bool> match, out ISpan? span)
    {
        span = null;
        if (transaction is TransactionTracer transactionTracer)
        {
            try
            {
                span = transactionTracer.Spans.SingleOrDefault(s => s is SpanTracer spanTracer && match(spanTracer));
            }
            catch (InvalidOperationException)
            {
                // If SingleOrDefault matches no element or more than one element
                return false;
            }
        }
        return span is not null;
    }

    protected static bool TryGetFirstSpanFromTraceData(ITransaction transaction, Func<SpanTracer, bool> match, out ISpan? span)
    {
        span = null;
        if (transaction is TransactionTracer transactionTracer)
        {
            span = transactionTracer.Spans
                .OrderBy(s => s.StartTimestamp)
                .FirstOrDefault(s => s is SpanTracer spanTracer && match(spanTracer));
        }
        return span is not null;
    }
}
