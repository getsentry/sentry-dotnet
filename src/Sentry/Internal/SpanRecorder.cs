namespace Sentry.Internal;

/// <summary>
/// Base <see cref="ISpanRecorder"/> for a node in a recorded transaction tree. Holds the owning
/// <see cref="TransactionTracer"/> (which new child spans are created on) and the node this recorder
/// represents (a <see cref="SpanTracer"/>, or the transaction tracer itself for the root), proxying metadata
/// straight through to it.
/// </summary>
internal abstract class SpanRecorderBase : ISpanRecorder
{
    /// <summary>The transaction that owns the whole tree; child spans are created on it.</summary>
    protected TransactionTracer Owner { get; }

    private readonly ISpan _node;

    protected SpanRecorderBase(TransactionTracer owner, ISpan node)
    {
        Owner = owner;
        _node = node;
    }

    public SpanId SpanId => _node.SpanId;

    public string? Description
    {
        get => _node.Description;
        set => _node.Description = value;
    }

    public SpanStatus? Status
    {
        get => _node.Status;
        set => _node.Status = value;
    }

    public void SetTag(string key, string value) => _node.SetTag(key, value);

    public void SetData(string key, object? value) => _node.SetData(key, value);

    public ISpanRecorder RecordSpan(
        string operation,
        DateTimeOffset startTimestamp,
        TimeSpan duration,
        SpanId? spanId = null,
        Action<ISpanRecorder>? configure = null)
    {
        if (duration < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(duration), duration, "Span duration cannot be negative.");
        }

        // Reuse the existing child-span machinery so the span is added to the transaction's flat span
        // collection with the correct parent pointer; then override the timing the tracer would otherwise
        // have measured with a stopwatch.
        var span = (SpanTracer)Owner.StartChild(spanId, SpanId, operation);
        span.StartTimestamp = startTimestamp;
        span.EndTimestamp = startTimestamp + duration;
        span.Status ??= SpanStatus.Ok;

        var recorder = new SpanRecorder(Owner, span);
        configure?.Invoke(recorder);
        return recorder;
    }
}

/// <summary>
/// <see cref="ISpanRecorder"/> backed by a <see cref="SpanTracer"/> whose timing has been set explicitly.
/// </summary>
internal sealed class SpanRecorder : SpanRecorderBase
{
    public SpanRecorder(TransactionTracer owner, SpanTracer span)
        : base(owner, span)
    {
    }
}

/// <summary>
/// <see cref="ITransactionRecorder"/> backed by a <see cref="TransactionTracer"/> whose timing has been set
/// explicitly. The tracer is never <c>Finish()</c>-ed (which would reset the live scope); instead the caller
/// converts it to a <see cref="SentryTransaction"/> and captures it directly.
/// </summary>
internal sealed class TransactionRecorder : SpanRecorderBase, ITransactionRecorder
{
    private readonly Scope? _scope;

    public TransactionRecorder(TransactionTracer tracer, Scope? scope)
        : base(tracer, tracer)
    {
        _scope = scope;
    }

    public SentryId TraceId => Owner.TraceId;

    public string? Release
    {
        get => Owner.Release;
        set => Owner.Release = value;
    }

    public string? Environment
    {
        get => Owner.Environment;
        set => Owner.Environment = value;
    }

    public void ConfigureScope(Action<Scope> configureScope)
    {
        // No scope when options are unavailable (e.g. a disabled hub); configuration is a no-op.
        if (_scope is not null)
        {
            configureScope(_scope);
        }
    }
}
