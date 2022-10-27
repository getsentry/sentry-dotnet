using System.ComponentModel;

namespace Sentry;

/// <summary>
/// Span.
/// </summary>
public interface ISpan : ISpanData
{
    /// <summary>
    /// Span description.
    /// </summary>
    // 'new' because it adds a setter.
    new string? Description { get; set; }

    /// <summary>
    /// Span operation.
    /// </summary>
    // 'new' because it adds a setter.
    new string Operation { get; set; }

    /// <summary>
    /// Span status.
    /// </summary>
    // 'new' because it adds a setter.
    new SpanStatus? Status { get; set; }

    /// <summary>
    /// Starts a child span.
    /// </summary>
    ISpan StartChild(string operation);

    /// <summary>
    /// Finishes the span.
    /// </summary>
    void Finish();

    /// <summary>
    /// Finishes the span with the specified status.
    /// </summary>
    void Finish(SpanStatus status);

    /// <summary>
    /// Finishes the span with the specified exception and status.
    /// </summary>
    void Finish(Exception exception, SpanStatus status);

    /// <summary>
    /// Finishes the span with the specified exception and automatically inferred status.
    /// </summary>
    void Finish(Exception exception);
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

    /// <summary>
    /// Gets the transaction that this span belongs to.
    /// </summary>
    public static ITransaction GetTransaction(this ISpan span) =>
        span switch
        {
            ITransaction transaction => transaction,
            SpanTracer tracer => tracer.Transaction,
            _ => throw new ArgumentOutOfRangeException(nameof(span), span, null)
        };
}
