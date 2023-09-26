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
        SpanId spanId,
        SpanId? parentSpanId,
        SentryId traceId,
        string name,
        string operation,
        string? description,
        SpanStatus? status,
        bool? isSampled,
        bool? isParentSampled,
        TransactionNameSource nameSource)
        : base(spanId, parentSpanId, traceId, operation, description, status, isSampled)
    {
        Name = name;
        IsParentSampled = isParentSampled;
        NameSource = nameSource;
    }

    /// <summary>
    /// Initializes an instance of <see cref="TransactionContext"/>.
    /// </summary>
    public TransactionContext(
        SpanId spanId,
        SpanId? parentSpanId,
        SentryId traceId,
        string name,
        string operation,
        string? description,
        SpanStatus? status,
        bool? isSampled,
        bool? isParentSampled)
        : base(spanId, parentSpanId, traceId, operation, description, status, isSampled)
    {
        Name = name;
        IsParentSampled = isParentSampled;
        NameSource = TransactionNameSource.Custom;
    }

    /// <summary>
    /// Initializes an instance of <see cref="TransactionContext"/>.
    /// </summary>
    public TransactionContext(
        SpanId? parentSpanId,
        SentryId traceId,
        string name,
        string operation,
        bool? isParentSampled)
        : this(SpanId.Create(), parentSpanId, traceId, name, operation, "", null, isParentSampled, isParentSampled)
    {
    }

    /// <summary>
    /// Initializes an instance of <see cref="TransactionContext"/>.
    /// </summary>
    public TransactionContext(
        string name,
        string operation,
        bool? isSampled)
        : this(SpanId.Create(), null, SentryId.Create(), name, operation, "", null, isSampled, null)
    {
    }

    /// <summary>
    /// Initializes an instance of <see cref="TransactionContext"/>.
    /// </summary>
    public TransactionContext(
        string name,
        string operation,
        SentryTraceHeader traceHeader)
        : this(SpanId.Create(), traceHeader.SpanId, traceHeader.TraceId, name, operation, "", null, traceHeader.IsSampled, traceHeader.IsSampled)
    {
    }

    /// <summary>
    /// Initializes an instance of <see cref="TransactionContext"/>.
    /// </summary>
    public TransactionContext(
        string name,
        string operation,
        SentryTraceHeader traceHeader,
        TransactionNameSource nameSource)
        : this(SpanId.Create(), traceHeader.SpanId, traceHeader.TraceId, name, operation, "", null, traceHeader.IsSampled, traceHeader.IsSampled, nameSource)
    {
    }

    /// <summary>
    /// Initializes an instance of <see cref="TransactionContext"/>.
    /// </summary>
    public TransactionContext(string name, string operation)
        : this(SpanId.Create(), null, SentryId.Create(), name, operation, "", null, null, null)
    {
    }

    /// <summary>
    /// Initializes an instance of <see cref="TransactionContext"/>.
    /// </summary>
    public TransactionContext(string name, string operation, TransactionNameSource nameSource)
        : this(SpanId.Create(), null, SentryId.Create(), name, operation, "", null, null, null, nameSource)
    {
    }
}
