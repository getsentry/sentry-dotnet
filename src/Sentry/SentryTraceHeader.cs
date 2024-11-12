namespace Sentry;

/// <summary>
/// Sentry trace header.
/// </summary>
public class SentryTraceHeader
{
    internal const string HttpHeaderName = "sentry-trace";

    internal static readonly SentryTraceHeader Empty = new(SentryId.Empty, SpanId.Empty, null);

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
    /// Initializes an instance of <see cref="SentryTraceHeader"/>.
    /// </summary>
    public SentryTraceHeader(SentryId traceId, SpanId spanSpanId, bool? isSampled)
    {
        TraceId = traceId;
        SpanId = spanSpanId;
        IsSampled = isSampled;
    }

    /// <inheritdoc />
    public override string ToString() => IsSampled is { } isSampled
        ? $"{TraceId}-{SpanId}-{(isSampled ? 1 : 0)}"
        : $"{TraceId}-{SpanId}";

    /// <summary>
    /// Parses <see cref="SentryTraceHeader"/> from string.
    /// </summary>
    public static SentryTraceHeader? Parse(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var components = value.Split('-', StringSplitOptions.RemoveEmptyEntries);
        if (components.Length < 2)
        {
            throw new FormatException($"Invalid Sentry trace header: {value}.");
        }

        var traceId = SentryId.Parse(components[0]);
        var spanId = SpanId.Parse(components[1]);

        var isSampled = components.Length >= 3
            ? string.Equals(components[2], "1", StringComparison.OrdinalIgnoreCase)
            : (bool?)null;

        return new SentryTraceHeader(traceId, spanId, isSampled);
    }
}
