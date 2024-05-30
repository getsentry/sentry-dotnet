using Sentry.Protocol;

namespace Sentry;

/// <summary>
/// Span metadata used for sampling.
/// </summary>
public class SpanContext : ITraceContext, ITraceContextInternal
{
    /// <inheritdoc />
    public SpanId SpanId { get; }

    /// <inheritdoc />
    public SpanId? ParentSpanId { get; }

    /// <inheritdoc />
    public SentryId TraceId { get; }

    /// <inheritdoc />
    public string Operation { get; set; }

    /// <inheritdoc />
    public string? Description { get; }

    /// <inheritdoc />
    public SpanStatus? Status { get; }

    /// <inheritdoc />
    public bool? IsSampled { get; }

    /// <summary>
    /// Identifies which instrumentation is being used.
    /// </summary>
    public Instrumenter Instrumenter { get; internal set; } = Instrumenter.Sentry;

    /// <inheritdoc/>
    public Origin? Origin { get; set; }

    /// <summary>
    /// Initializes an instance of <see cref="SpanContext"/>.
    /// </summary>
    public SpanContext(
        string operation,
        SpanId? spanId = null,
        SpanId? parentSpanId = null,
        SentryId? traceId = null,
        string? description = null,
        SpanStatus? status = null,
        bool? isSampled = null)
    {
        SpanId = spanId ?? SpanId.Create();
        ParentSpanId = parentSpanId;
        TraceId = traceId ?? SentryId.Create();
        Operation = operation;
        Description = description;
        Status = status;
        IsSampled = isSampled;
    }
}
