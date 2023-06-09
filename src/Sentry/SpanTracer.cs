using Sentry.Internal;

namespace Sentry;

/// <summary>
/// Transaction span tracer.
/// </summary>
public class SpanTracer : ISpan
{
    private readonly IHub _hub;
    private readonly SentryStopwatch _stopwatch = SentryStopwatch.StartNew();

    internal TransactionTracer Transaction { get; }

    /// <inheritdoc />
    public SpanId SpanId { get; internal set; }

    /// <inheritdoc />
    public SpanId? ParentSpanId { get; }

    /// <inheritdoc />
    public SentryId TraceId { get; }

    /// <inheritdoc />
    public DateTimeOffset StartTimestamp { get; internal set; }

    /// <inheritdoc />
    public DateTimeOffset? EndTimestamp { get; internal set; }

    /// <inheritdoc />
    public bool IsFinished => EndTimestamp is not null;

    /// <inheritdoc cref="ISpan.Operation" />
    public string Operation { get; set; }

    /// <inheritdoc cref="ISpan.Description" />
    public string? Description { get; set; }

    /// <inheritdoc cref="ISpan.Status" />
    public SpanStatus? Status { get; set; }

    /// <inheritdoc />
    public bool? IsSampled { get; internal set; }

    private ConcurrentDictionary<string, string>? _tags;

    /// <inheritdoc />
    public IReadOnlyDictionary<string, string> Tags => _tags ??= new ConcurrentDictionary<string, string>();

    /// <inheritdoc />
    public void SetTag(string key, string value) =>
        (_tags ??= new ConcurrentDictionary<string, string>())[key] = value;

    /// <inheritdoc />
    public void UnsetTag(string key) =>
        (_tags ??= new ConcurrentDictionary<string, string>()).TryRemove(key, out _);

    private readonly ConcurrentDictionary<string, object?> _data = new();

    /// <inheritdoc />
    public IReadOnlyDictionary<string, object?> Extra => _data;

    /// <inheritdoc />
    public void SetExtra(string key, object? value) => _data[key] = value;

    /// <summary>
    /// Initializes an instance of <see cref="SpanTracer"/>.
    /// </summary>
    public SpanTracer(
        IHub hub,
        TransactionTracer transaction,
        SpanId? parentSpanId,
        SentryId traceId,
        string operation)
    {
        _hub = hub;
        Transaction = transaction;
        SpanId = SpanId.Create();
        ParentSpanId = parentSpanId;
        TraceId = traceId;
        Operation = operation;
        StartTimestamp = _stopwatch.StartDateTimeOffset;
    }

    internal SpanTracer(
        IHub hub,
        TransactionTracer transaction,
        SpanId spanId,
        SpanId? parentSpanId,
        SentryId traceId,
        string operation)
    {
        _hub = hub;
        Transaction = transaction;
        SpanId = spanId;
        ParentSpanId = parentSpanId;
        TraceId = traceId;
        Operation = operation;
        StartTimestamp = _stopwatch.StartDateTimeOffset;
    }

    /// <inheritdoc />
    public ISpan StartChild(string operation) => Transaction.StartChild(SpanId, operation);

    /// <summary>
    /// Used to mark a span as unfinished when it was previously marked as finished. This allows us to reuse spans for
    /// DB Connections that get reused by the underlying connection pool
    /// </summary>
    internal void Unfinish()
    {
        Status = null;
        EndTimestamp = null;
    }

    /// <inheritdoc />
    public void Finish()
    {
        Status ??= SpanStatus.Ok;
        EndTimestamp ??= _stopwatch.CurrentDateTimeOffset;
    }

    /// <inheritdoc />
    public void Finish(SpanStatus status)
    {
        Status = status;
        Finish();
    }

    /// <inheritdoc />
    public void Finish(Exception exception, SpanStatus status)
    {
        _hub.BindException(exception, this);
        Finish(status);
    }

    /// <inheritdoc />
    public void Finish(Exception exception) => Finish(exception, SpanStatusConverter.FromException(exception));

    /// <inheritdoc />
    public SentryTraceHeader GetTraceHeader() => new(TraceId, SpanId, IsSampled);
}
