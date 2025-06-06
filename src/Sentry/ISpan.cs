using Sentry.Internal;

namespace Sentry;

/// <summary>
/// SpanTracer interface
/// </summary>
public interface ISpan : ISpanData
{
    /// <summary>
    /// Span description.
    /// </summary>
    // 'new' because it adds a setter.
    public new string? Description { get; set; }

    /// <summary>
    /// Span operation.
    /// </summary>
    // 'new' because it adds a setter.
    public new string Operation { get; set; }

    /// <summary>
    /// Span status.
    /// </summary>
    // 'new' because it adds a setter.
    public new SpanStatus? Status { get; set; }

    /// <summary>
    /// Starts a child span.
    /// </summary>
    public ISpan StartChild(string operation);

    /// <summary>
    /// Finishes the span.
    /// </summary>
    public void Finish();

    /// <summary>
    /// Finishes the span with the specified status.
    /// </summary>
    public void Finish(SpanStatus status);

    /// <summary>
    /// Finishes the span with the specified exception and status.
    /// </summary>
    public void Finish(Exception exception, SpanStatus status);

    /// <summary>
    /// Finishes the span with the specified exception and automatically inferred status.
    /// </summary>
    public void Finish(Exception exception);
}

/// <summary>
/// Extensions for <see cref="ISpan"/>.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class SpanExtensions
{
    /// <summary>
    /// Starts a child span.
    /// </summary>
    public static ISpan StartChild(this ISpan span, string operation, string? description)
    {
        var child = span.StartChild(operation);
        child.Description = description;

        return child;
    }

    internal static ISpan StartChild(this ISpan span, SpanContext context)
    {
        var transaction = span.GetTransaction();
        if (transaction is TransactionTracer transactionTracer)
        {
            var child = transactionTracer.StartChild(context.SpanId, span.SpanId, context.Operation, context.Instrumenter);
            if (child is SpanTracer childTracer)
            {
                childTracer.Description = context.Description;
                return childTracer;
            }
        }
        if (transaction is UnsampledTransaction unsampledTransaction)
        {
            return unsampledTransaction.StartChild(context.Operation, context.SpanId);
        }
        return NoOpSpan.Instance;
    }

    /// <summary>
    /// Gets the transaction that this span belongs to.
    /// </summary>
    public static ITransactionTracer GetTransaction(this ISpan span) =>
        span switch
        {
            ITransactionTracer transaction => transaction,
            UnsampledSpan unsampledSpan => unsampledSpan.Transaction,
            SpanTracer spanTracer => spanTracer.Transaction,
            _ => throw new ArgumentOutOfRangeException(nameof(span), span, null)
        };

    /// <summary>
    /// Gets the parent span for database operations. This is the last active non-database span, which might be the
    /// transaction root, or it might be some other child span of the transaction (such as a web request).
    /// </summary>
    /// <remarks>
    /// Used by EF, EF Core, and SQLClient integrations.
    /// </remarks>
    internal static ISpan GetDbParentSpan(this ISpan span)
    {
        var transaction = span.GetTransaction();
        return transaction.Spans
                   .OrderByDescending(x => x.StartTimestamp)
                   .FirstOrDefault(s => !s.IsFinished && !s.Operation.StartsWith("db."))
               ?? transaction;
    }
}
