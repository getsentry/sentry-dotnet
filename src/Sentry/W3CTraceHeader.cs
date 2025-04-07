namespace Sentry;

/// <summary>
/// Extension methods for working with Sentry trace headers.
/// </summary>
internal class W3CTraceHeader
{
    private const string SupportedVersion = "00";

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
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source), "Source Sentry trace header cannot be null.");
        }

        SentryTraceHeader = source;
    }

    /// <summary>
    /// Gets the Sentry trace header containing trace identification and sampling information.
    /// </summary>
    /// <value>
    /// The Sentry trace header that contains the trace ID, span ID, and sampling decision.
    /// </value>
    public SentryTraceHeader SentryTraceHeader { get; }

    /// <summary>
    /// Parses a <see cref="SentryTraceHeader"/> from a string representation of the Sentry trace header.
    /// </summary>
    /// <param name="value">
    /// A string containing the Sentry trace header, expected to follow the format "traceId-spanId-sampled",
    /// where "sampled" is optional.
    /// </param>
    /// <returns>
    /// A <see cref="SentryTraceHeader"/> object if parsing succeeds, or <c>null</c> if the input string is null, empty, or whitespace.
    /// </returns>
    /// <exception cref="FormatException">
    /// Thrown if the input string does not contain a valid trace header format, specifically if it lacks required trace ID and span ID components.
    /// </exception>
    public static W3CTraceHeader? Parse(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var components = value.Split('-', StringSplitOptions.RemoveEmptyEntries);
        if (components.Length < 4)
        {
            throw new FormatException($"Invalid W3C trace header: {value}.");
        }

        var version = components[0];
        if (version != SupportedVersion)
        {
            throw new FormatException($"Invalid W3C trace header version: {version}.");
        }

        var traceId = SentryId.Parse(components[1]);
        var spanId = SpanId.Parse(components[2]);

        var isSampled = string.Equals(components[3], "01", StringComparison.OrdinalIgnoreCase);

        return new W3CTraceHeader(new SentryTraceHeader(traceId, spanId, isSampled));
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        var traceFlags = ConvertSampledToTraceFlags(SentryTraceHeader.IsSampled);
        return $"{SupportedVersion}-{SentryTraceHeader.TraceId}-{SentryTraceHeader.SpanId}-{traceFlags}";
    }

    private static string? ConvertSampledToTraceFlags(bool? isSampled) => isSampled ?? false ? "01" : "00";
}
