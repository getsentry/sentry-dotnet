namespace Sentry;

/// <summary>
/// Extension methods for working with Sentry trace headers.
/// </summary>
internal class W3CTraceHeader
{
    /// <summary>
    /// The name of the W3C trace context header used for distributed tracing.
    /// This field contains the value "traceparent" which is part of the W3C Trace Context specification.
    /// </summary>
    public const string HttpHeaderName = "traceparent";

    /// <summary>
    /// Initializes a new instance of the <see cref="W3CTraceHeader"/> class from a Sentry trace header.
    /// </summary>
    /// <param name="source">The source Sentry trace header to create the W3C trace header from.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
    public W3CTraceHeader(SentryTraceHeader source)
    {
        ArgumentNullException.ThrowIfNull(source);

        SentryTraceHeader = source;
    }

    /// <summary>
    /// Gets the Sentry trace header containing trace identification and sampling information.
    /// </summary>
    /// <value>
    /// The Sentry trace header that contains the trace ID, span ID, and sampling decision.
    /// </value>
    public SentryTraceHeader SentryTraceHeader { get; }

    /// <inheritdoc/>
    public override string ToString()
    {
        const string version = "00";
        var traceFlags = ConvertSampledToTraceFlags(SentryTraceHeader.IsSampled);
        if (traceFlags is null)
        {
            return $"{version}-{SentryTraceHeader.TraceId}-{SentryTraceHeader.SpanId}";
        }

        return $"{version}-{SentryTraceHeader.TraceId}-{SentryTraceHeader.SpanId}-{traceFlags}";
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        if (obj is W3CTraceHeader other)
        {
            return SentryTraceHeader.Equals(other.SentryTraceHeader);
        }

        return false;
    }

    /// <inheritdoc/>
    public override int GetHashCode() => SentryTraceHeader.GetHashCode();

    private static string? ConvertSampledToTraceFlags(bool? isSampled)
    {
        return isSampled switch
        {
            true => "01",
            false => "00",
            null => null
        };
    }
}
