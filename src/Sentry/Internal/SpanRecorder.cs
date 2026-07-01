namespace Sentry.Internal;

/// <summary>
/// Shared logic for materializing a recorded child span onto an existing <see cref="TransactionTracer"/>.
/// </summary>
internal static class SpanRecorderInternals
{
    public static SpanRecorder RecordChild(
        TransactionTracer owner,
        SpanId parentSpanId,
        string operation,
        DateTimeOffset startTimestamp,
        TimeSpan duration,
        SpanId? spanId,
        Action<ISpanRecorder>? configure)
    {
        if (duration < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(duration), duration, "Span duration cannot be negative.");
        }

        // Reuse the existing child-span machinery so the span is added to the transaction's flat span
        // collection with the correct parent pointer; then override the timing the tracer would otherwise
        // have measured with a stopwatch.
        var span = (SpanTracer)owner.StartChild(spanId, parentSpanId, operation);
        span.StartTimestamp = startTimestamp;
        span.EndTimestamp = startTimestamp + duration;
        span.Status ??= SpanStatus.Ok;

        var recorder = new SpanRecorder(owner, span);
        configure?.Invoke(recorder);
        return recorder;
    }
}

/// <summary>
/// <see cref="ISpanRecorder"/> backed by a <see cref="SpanTracer"/> whose timing has been set explicitly.
/// </summary>
internal sealed class SpanRecorder : ISpanRecorder
{
    private readonly TransactionTracer _owner;
    private readonly SpanTracer _span;

    public SpanRecorder(TransactionTracer owner, SpanTracer span)
    {
        _owner = owner;
        _span = span;
    }

    public SpanId SpanId => _span.SpanId;

    public string? Description
    {
        get => _span.Description;
        set => _span.Description = value;
    }

    public SpanStatus? Status
    {
        get => _span.Status;
        set => _span.Status = value;
    }

    public void SetTag(string key, string value) => _span.SetTag(key, value);

    public void SetData(string key, object? value) => _span.SetData(key, value);

    public ISpanRecorder RecordSpan(
        string operation,
        DateTimeOffset startTimestamp,
        TimeSpan duration,
        SpanId? spanId = null,
        Action<ISpanRecorder>? configure = null) =>
        SpanRecorderInternals.RecordChild(_owner, _span.SpanId, operation, startTimestamp, duration, spanId, configure);
}

/// <summary>
/// <see cref="ITransactionRecorder"/> backed by a <see cref="TransactionTracer"/> whose timing has been set
/// explicitly. The tracer is never <c>Finish()</c>-ed (which would reset the live scope); instead the caller
/// converts it to a <see cref="SentryTransaction"/> and captures it directly.
/// </summary>
internal sealed class TransactionRecorder : ITransactionRecorder
{
    private readonly TransactionTracer _tracer;
    private readonly Scope? _scope;

    public TransactionRecorder(TransactionTracer tracer, Scope? scope)
    {
        _tracer = tracer;
        _scope = scope;
    }

    internal TransactionTracer Tracer => _tracer;

    public void ConfigureScope(Action<Scope> configureScope)
    {
        // No scope when options are unavailable (e.g. a disabled hub); configuration is a no-op.
        if (_scope is not null)
        {
            configureScope(_scope);
        }
    }

    public SpanId SpanId => _tracer.SpanId;

    public SentryId TraceId => _tracer.TraceId;

    public string? Description
    {
        get => _tracer.Description;
        set => _tracer.Description = value;
    }

    public SpanStatus? Status
    {
        get => _tracer.Status;
        set => _tracer.Status = value;
    }

    public string? Release
    {
        get => _tracer.Release;
        set => _tracer.Release = value;
    }

    public string? Environment
    {
        get => _tracer.Environment;
        set => _tracer.Environment = value;
    }

    public void SetTag(string key, string value) => _tracer.SetTag(key, value);

    public void SetData(string key, object? value) => _tracer.SetData(key, value);

    public ISpanRecorder RecordSpan(
        string operation,
        DateTimeOffset startTimestamp,
        TimeSpan duration,
        SpanId? spanId = null,
        Action<ISpanRecorder>? configure = null) =>
        SpanRecorderInternals.RecordChild(_tracer, _tracer.SpanId, operation, startTimestamp, duration, spanId, configure);
}
