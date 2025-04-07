using Sentry.Protocol;

namespace Sentry.Internal;

/// <summary>
/// Span class to use when we can't return null but a request to create a span couldn't be completed.
/// </summary>
internal class NoOpSpan : ISpan
{
    public static ISpan Instance { get; } = new NoOpSpan();

    protected NoOpSpan()
    {
    }

    public SpanId SpanId => SpanId.Empty;
    public SpanId? ParentSpanId => SpanId.Empty;
    public SentryId TraceId => SentryId.Empty;
    public bool? IsSampled => default;
    public IReadOnlyDictionary<string, string> Tags => ImmutableDictionary<string, string>.Empty;
    public IReadOnlyDictionary<string, object?> Extra => ImmutableDictionary<string, object?>.Empty;
    public IReadOnlyDictionary<string, object?> Data => ImmutableDictionary<string, object?>.Empty;
    public DateTimeOffset StartTimestamp => default;
    public DateTimeOffset? EndTimestamp => default;
    public bool IsFinished => default;

    public string Operation
    {
        get => string.Empty;
        set { }
    }

    public event EventHandler<SpanStatus?>? StatusChanged
    {
        add { }
        remove { }
    }

    public string? Description
    {
        get => default;
        set { }
    }

    public SpanStatus? Status
    {
        get => default;
        set { }
    }

    public ISpan StartChild(string operation, DateTimeOffset? startTime) => this;

    public void Finish(DateTimeOffset? timestamp = null)
    {
    }

    public void Finish(SpanStatus status, DateTimeOffset? timestamp = null)
    {
    }

    public void Finish(Exception exception, SpanStatus status, DateTimeOffset? timestamp = null)
    {
    }

    public void Finish(Exception exception, DateTimeOffset? timestamp = null)
    {
    }

    public void SetTag(string key, string value)
    {
    }

    public void UnsetTag(string key)
    {
    }

    public void SetExtra(string key, object? value)
    {
    }

    public void SetData(string key, object? value)
    {
    }

    public SentryTraceHeader GetTraceHeader() => SentryTraceHeader.Empty;

    public IReadOnlyDictionary<string, Measurement> Measurements => ImmutableDictionary<string, Measurement>.Empty;

    public void SetMeasurement(string name, Measurement measurement)
    {
    }

    public string? Origin { get; set; }
}
