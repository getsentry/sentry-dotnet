namespace Sentry;

/// <summary>
/// Transaction metadata used for sampling.
/// </summary>
public class TransactionContext : SpanContext, ITransactionContext
{
    /// <inheritdoc />
    public string Name { get; set; }

    /// <inheritdoc />
    public TransactionNameSource NameSource { get; set; }

    /// <summary>
    /// Whether the parent transaction of this transaction has been sampled.
    /// </summary>
    public bool? IsParentSampled { get; }

    /// <summary>
    /// Initializes an instance of <see cref="TransactionContext"/>.
    /// </summary>
    public TransactionContext(
        string name,
        string operation,
        SpanId? spanId = null,
        SpanId? parentSpanId = null,
        SentryId? traceId = null,
        string? description = "",
        SpanStatus? status = null,
        bool? isSampled = null,
        bool? isParentSampled = null,
        TransactionNameSource nameSource = TransactionNameSource.Custom
    )
        : base(operation, spanId, parentSpanId, traceId, description, status, isSampled)
    {
        Name = name;
        IsParentSampled = isParentSampled;
        NameSource = nameSource;
    }

    /// <summary>
    /// Initializes an instance of <see cref="TransactionContext"/>.
    /// </summary>
    internal TransactionContext(
        string name,
        string operation,
        SentryTraceHeader traceHeader)
        : this(name, operation, SpanId.Create(), parentSpanId: traceHeader.SpanId, traceId: traceHeader.TraceId, "", null, isSampled: traceHeader.IsSampled, isParentSampled: traceHeader.IsSampled)
    {
    }
}
