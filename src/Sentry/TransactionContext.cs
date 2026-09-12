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
    /// The Dynamic Sampling Context received from the upstream SDK, when this context was created by
    /// <see cref="IHub.ContinueTrace(SentryTraceHeader?, BaggageHeader?, string?, string?)"/> from a baggage header.
    /// A transaction started from this context propagates it unchanged instead of creating a new one, so that
    /// <c>sample_rand</c> and the other frozen items stay the same across the whole trace.
    /// </summary>
    /// <seealso href="https://develop.sentry.dev/sdk/telemetry/traces/dynamic-sampling-context/#unified-propagation-mechanism"/>
    internal DynamicSamplingContext? DynamicSamplingContext { get; set; }

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
