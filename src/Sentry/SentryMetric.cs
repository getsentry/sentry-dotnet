using Sentry.Extensibility;
using Sentry.Protocol;

namespace Sentry;

/// <summary>
/// Represents a Sentry Trace-connected Metric.
/// </summary>
/// <remarks>
/// Sentry Docs: <see href="https://docs.sentry.io/product/explore/metrics/"/>.
/// Sentry Developer Documentation: <see href="https://develop.sentry.dev/sdk/telemetry/metrics/"/>.
/// Sentry .NET SDK Docs: <see href="https://docs.sentry.io/platforms/dotnet/metrics/"/>.
/// </remarks>
[DebuggerDisplay(@"SentryMetric \{ Type = {Type}, Name = '{Name}', Value = {Value} \}")]
public abstract partial class SentryMetric
{
    [SetsRequiredMembers]
    private protected SentryMetric(DateTimeOffset timestamp, SentryId traceId, SentryMetricType type, string name)
    {
        Timestamp = timestamp;
        TraceId = traceId;
        Type = type;
        Name = name;
        // 7 is the number of built-in attributes, so we start with that.
        Attributes = new SentryAttributes(7);
    }

    /// <summary>
    /// Timestamp indicating when the metric was recorded.
    /// </summary>
    /// <remarks>
    /// Sent as seconds since the Unix epoch.
    /// </remarks>
    public required DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// The trace id of the metric.
    /// </summary>
    public required SentryId TraceId { get; init; }

    /// <summary>
    /// The type of metric.
    /// </summary>
    /// <remarks>
    /// One of:
    /// <list type="table">
    ///   <listheader>
    ///     <term>Type</term>
    ///     <description>Description</description>
    ///   </listheader>
    ///   <item>
    ///     <term>counter</term>
    ///     <description>A metric that increments counts.</description>
    ///   </item>
    ///   <item>
    ///     <term>gauge</term>
    ///     <description>A metric that tracks a value that can go up or down.</description>
    ///   </item>
    ///   <item>
    ///     <term>distribution</term>
    ///     <description>A metric that tracks the statistical distribution of values.</description>
    ///   </item>
    /// </list>
    /// </remarks>
    public required SentryMetricType Type { get; init; }

    /// <summary>
    /// The name of the metric.
    /// </summary>
    /// <remarks>
    /// This should follow a hierarchical naming convention using dots as separators (e.g., api.response_time, db.query.duration).
    /// </remarks>
    public required string Name { get; init; }

    /// <summary>
    /// The numeric value of the metric.
    /// </summary>
    /// <remarks>
    /// The interpretation depends on the metric type:
    /// <list type="table">
    ///   <listheader>
    ///     <term>Type</term>
    ///     <description>Description</description>
    ///   </listheader>
    ///   <item>
    ///     <term>counter</term>
    ///     <description>The count to increment by (should default to <see langword="1"/>).</description>
    ///   </item>
    ///   <item>
    ///     <term>gauge</term>
    ///     <description>The current value.</description>
    ///   </item>
    ///   <item>
    ///     <term>distribution</term>
    ///     <description>A single measured value.</description>
    ///   </item>
    /// </list>
    /// </remarks>
    // Internal non-generic (boxed to object) read-only property for testing, and usage in DebuggerDisplayAttribute.
    internal abstract object Value { get; }

    /// <summary>
    /// The span id of the span that was active when the metric was emitted.
    /// </summary>
    public SpanId? SpanId { get; init; }

    /// <summary>
    /// The unit of measurement for the metric value.
    /// </summary>
    /// <remarks>
    /// Only used for <see cref="SentryMetricType.Gauge"/> and <see cref="SentryMetricType.Distribution"/>.
    /// </remarks>
    public string? Unit { get; init; }

    internal SentryAttributes Attributes { get; }

    /// <summary>
    /// Gets the metric value if it is of the specified type <typeparamref name="TValue"/>.
    /// </summary>
    /// <param name="value">When this method returns, contains the metric value, if it is of the specified type <typeparamref name="TValue"/>; otherwise, the <see langword="default"/> value for the type of the <paramref name="value"/> parameter. This parameter is passed uninitialized.</param>
    /// <typeparam name="TValue">The numeric type of the metric.</typeparam>
    /// <returns><see langword="true"/> if this <see cref="SentryMetric"/> is of type <typeparamref name="TValue"/>; otherwise, <see langword="false"/>.</returns>
    /// <remarks>Supported numeric value types for <typeparamref name="TValue"/> are <see langword="byte"/>, <see langword="short"/>, <see langword="int"/>, <see langword="long"/>, <see langword="float"/>, and <see langword="double"/>.</remarks>
    public abstract bool TryGetValue<TValue>(out TValue value) where TValue : struct;

    /// <summary>
    /// Gets the attribute value associated with the specified key.
    /// </summary>
    /// <remarks>
    /// Returns <see langword="true"/> if this <see cref="SentryMetric"/> contains an attribute with the specified key which is of type <typeparamref name="TAttribute"/> and it's value is not <see langword="null"/>.
    /// Otherwise <see langword="false"/>.
    /// Supported types:
    /// <list type="table">
    ///   <listheader>
    ///     <term>Type</term>
    ///     <description>Range</description>
    ///   </listheader>
    ///   <item>
    ///     <term>string</term>
    ///     <description><see langword="string"/> and <see langword="char"/></description>
    ///   </item>
    ///   <item>
    ///     <term>boolean</term>
    ///     <description><see langword="false"/> and <see langword="true"/></description>
    ///   </item>
    ///   <item>
    ///     <term>integer</term>
    ///     <description>64-bit signed integral numeric types</description>
    ///   </item>
    ///   <item>
    ///     <term>double</term>
    ///     <description>64-bit floating-point numeric types</description>
    ///   </item>
    /// </list>
    /// Unsupported types:
    /// <list type="table">
    ///   <listheader>
    ///     <term>Type</term>
    ///     <description>Result</description>
    ///   </listheader>
    ///   <item>
    ///     <term><see langword="object"/></term>
    ///     <description><c>ToString</c> as <c>"type": "string"</c></description>
    ///   </item>
    ///   <item>
    ///     <term>Collections</term>
    ///     <description><c>ToString</c> as <c>"type": "string"</c></description>
    ///   </item>
    ///   <item>
    ///     <term><see langword="null"/></term>
    ///     <description>ignored</description>
    ///   </item>
    /// </list>
    /// </remarks>
    /// <seealso href="https://develop.sentry.dev/sdk/telemetry/metrics/"/>
    public bool TryGetAttribute<TAttribute>(string key, [MaybeNullWhen(false)] out TAttribute value)
        => Attributes.TryGetAttribute(key, out value);

    /// <summary>
    /// Set a key-value pair of data attached to the metric.
    /// </summary>
    public void SetAttribute<TAttribute>(string key, TAttribute value) where TAttribute : notnull
        => Attributes.SetAttribute(key, value);

    /// <inheritdoc cref="ISentryJsonSerializable.WriteTo(Utf8JsonWriter, IDiagnosticLogger)" />
    internal void WriteTo(Utf8JsonWriter writer, IDiagnosticLogger? logger)
    {
        writer.WriteStartObject();

#if NET9_0_OR_GREATER
        writer.WriteNumber("timestamp", Timestamp.ToUnixTimeMilliseconds() / (double)TimeSpan.MillisecondsPerSecond);
#else
        writer.WriteNumber("timestamp", Timestamp.ToUnixTimeMilliseconds() / 1_000.0);
#endif

        writer.WriteString("type", Type.ToProtocolString(logger));
        writer.WriteString("name", Name);
        WriteMetricValueTo(writer, logger);

        writer.WritePropertyName("trace_id");
        TraceId.WriteTo(writer, logger);

        if (SpanId.HasValue)
        {
            writer.WritePropertyName("span_id");
            SpanId.Value.WriteTo(writer, logger);
        }

        if (!string.IsNullOrEmpty(Unit))
        {
            writer.WriteString("unit", Unit);
        }

        writer.WritePropertyName("attributes");
        Attributes.WriteTo(writer, logger);

        writer.WriteEndObject();
    }

    private protected abstract void WriteMetricValueTo(Utf8JsonWriter writer, IDiagnosticLogger? logger);
}
