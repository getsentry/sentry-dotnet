namespace Sentry;

/// <summary>
/// W3C Trace Context traceparent header.
/// </summary>
public class W3CTraceparentHeader
{
    internal const string HttpHeaderName = "traceparent";

    /// <summary>
    /// Trace ID.
    /// </summary>
    public SentryId TraceId { get; }

    /// <summary>
    /// Span ID.
    /// </summary>
    public SpanId SpanId { get; }

    /// <summary>
    /// Whether the trace is sampled.
    /// </summary>
    public bool? IsSampled { get; }

    /// <summary>
    /// Initializes an instance of <see cref="W3CTraceparentHeader"/>.
    /// </summary>
    public W3CTraceparentHeader(SentryId traceId, SpanId spanId, bool? isSampled)
    {
        TraceId = traceId;
        SpanId = spanId;
        IsSampled = isSampled;
    }

    /// <inheritdoc />
    public override string ToString() => IsSampled is { } isSampled
        ? $"00-{TraceId}-{SpanId}-{(isSampled ? "01" : "00")}"
        : $"00-{TraceId}-{SpanId}-00";
}
