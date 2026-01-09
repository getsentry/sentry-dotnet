using Sentry.Extensibility;
using Sentry.Protocol;

namespace Sentry;

/// <summary>
/// Represents a Sentry Trace-connected Metric.
/// </summary>
/// <typeparam name="T">The numeric type of the metric.</typeparam>
/// <remarks>
/// Sentry Docs: <see href="https://docs.sentry.io/product/explore/metrics/"/>.
/// Sentry Developer Documentation: <see href="https://develop.sentry.dev/sdk/telemetry/metrics/"/>.
/// Sentry .NET SDK Docs: <see href="https://docs.sentry.io/platforms/dotnet/metrics/"/>.
/// </remarks>
[DebuggerDisplay(@"SentryMetric \{ Type = {Type}, Name = '{Name}', Value = {Value} \}")]
public sealed class SentryMetric<T> : ISentryMetric where T : struct
{
    private Dictionary<string, object>? _attributes;

    [SetsRequiredMembers]
    internal SentryMetric(DateTimeOffset timestamp, SentryId traceId, SentryMetricType type, string name, T value)
    {
        Timestamp = timestamp;
        TraceId = traceId;
        Type = type;
        Name = name;
        Value = value;
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
    /// <remarks>
    /// The value should be 16 random bytes encoded as a hex string (32 characters long).
    /// The trace id should be grabbed from the current propagation context in the SDK.
    /// </remarks>
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
    public required T Value { get; init; }

    /// <summary>
    /// The span id of the span that was active when the metric was emitted.
    /// </summary>
    /// <remarks>
    /// The value should be 8 random bytes encoded as a hex string (16 characters long).
    /// The span id should be grabbed from the current active span in the SDK.
    /// </remarks>
    public SpanId? SpanId { get; init; }

    /// <summary>
    /// The unit of measurement for the metric value.
    /// </summary>
    /// <remarks>
    /// Only used for <see cref="SentryMetricType.Gauge"/> and <see cref="SentryMetricType.Distribution"/>.
    /// </remarks>
    public string? Unit { get; init; }

    /// <summary>
    /// A dictionary of key-value pairs of arbitrary data attached to the metric.
    /// </summary>
    /// <remarks>
    /// Attributes must also declare the type of the value.
    /// Supported Types:
    /// <list type="table">
    ///   <listheader>
    ///     <term>Type</term>
    ///     <description>Comment</description>
    ///   </listheader>
    ///   <item>
    ///     <term>string</term>
    ///     <description></description>
    ///   </item>
    ///   <item>
    ///     <term>boolean</term>
    ///     <description></description>
    ///   </item>
    ///   <item>
    ///     <term>integer</term>
    ///     <description>64-bit signed integer</description>
    ///   </item>
    ///   <item>
    ///     <term>double</term>
    ///     <description>64-bit floating point number</description>
    ///   </item>
    /// </list>
    /// Integers should be 64-bit signed integers.
    /// For 64-bit unsigned integers, use the string type to avoid overflow issues until unsigned integers are natively supported.
    /// </remarks>
    public IReadOnlyDictionary<string, object> Attributes
    {
        get
        {
#if NET8_0_OR_GREATER
            return _attributes ?? (IReadOnlyDictionary<string, object>)ReadOnlyDictionary<string, object>.Empty;
#else
            return _attributes ?? EmptyAttributes;
#endif
        }
    }

    /// <summary>
    /// Set a key-value pair of data attached to the metric.
    /// </summary>
    public void SetAttribute(string key, object value)
    {
        _attributes ??= new Dictionary<string, object>();

        _attributes[key] = new SentryAttribute(value);
    }

    internal void SetAttributes(IEnumerable<KeyValuePair<string, object>>? attributes)
    {
        if (attributes is null)
        {
            return;
        }

        if (_attributes is null)
        {
            if (attributes.TryGetNonEnumeratedCount(out var count))
            {
                _attributes = new Dictionary<string, object>(count);
            }
            else
            {
                _attributes = new Dictionary<string, object>();
            }
        }
        else
        {
#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            if (attributes.TryGetNonEnumeratedCount(out var count))
            {
                _ = _attributes.EnsureCapacity(_attributes.Count + count);
            }
#endif
        }

        foreach (var attribute in attributes)
        {
            _attributes[attribute.Key] = attribute.Value;
        }
    }

    internal void SetAttributes(ReadOnlySpan<KeyValuePair<string, object>> attributes)
    {
        if (attributes.IsEmpty)
        {
            return;
        }

        if (_attributes is null)
        {
            _attributes = new Dictionary<string, object>(attributes.Length);
        }
        else
        {
#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            _ = _attributes.EnsureCapacity(_attributes.Count + attributes.Length);
#endif
        }

        foreach (var attribute in attributes)
        {
            _attributes[attribute.Key] = attribute.Value;
        }
    }

    internal void Apply(Scope? scope)
    {
    }

    void ISentryMetric.WriteTo(Utf8JsonWriter writer, IDiagnosticLogger? logger)
    {
        writer.WriteStartObject();

#if NET9_0_OR_GREATER
        writer.WriteNumber("timestamp", Timestamp.ToUnixTimeMilliseconds() / (double)TimeSpan.MillisecondsPerSecond);
#else
        writer.WriteNumber("timestamp", Timestamp.ToUnixTimeMilliseconds() / 1_000.0);
#endif

        writer.WriteString("type", Type.ToProtocolString(logger));
        writer.WriteString("name", Name);
        writer.WriteMetricValue("value", Value);

        writer.WritePropertyName("trace_id");
        TraceId.WriteTo(writer, logger);

        if (SpanId.HasValue)
        {
            writer.WritePropertyName("span_id");
            SpanId.Value.WriteTo(writer, logger);
        }

        if (Unit is not null)
        {
            writer.WriteString("unit", Unit);
        }

        if (_attributes is not null && _attributes.Count != 0)
        {
            writer.WritePropertyName("attributes");
            writer.WriteStartObject();

            foreach (var attribute in _attributes)
            {
                SentryAttributeSerializer.WriteAttribute(writer, attribute.Key, attribute.Value, logger);
            }

            writer.WriteEndObject();
        }

        writer.WriteEndObject();
    }

#if !NET8_0_OR_GREATER
    private static IReadOnlyDictionary<string, object> EmptyAttributes { get; } = new ReadOnlyDictionary<string, object>(new Dictionary<string, object>());
#endif
}

// TODO: remove after upgrading <LangVersion>14.0</LangVersion> and updating <PackageReference Include="Polyfill" />
#if !NET6_0_OR_GREATER
file static class EnumerableExtensions
{
    internal static bool TryGetNonEnumeratedCount<TSource>(this IEnumerable<TSource> source, out int count)
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        if (source is ICollection<TSource> genericCollection)
        {
            count = genericCollection.Count;
            return true;
        }

        if (source is ICollection collection)
        {
            count = collection.Count;
            return true;
        }

        count = 0;
        return false;
    }
}
#endif

file static class Utf8JsonWriterExtensions
{
    //TODO: Integers should be a 64-bit signed integer, while doubles should be a 64-bit floating point number.
    internal static void WriteMetricValue<T>(this Utf8JsonWriter writer, string propertyName, T value) where T : struct
    {
        var type = typeof(T);

        if (type == typeof(byte))
        {
            writer.WriteNumber(propertyName, (byte)(object)value);
        }
        else if (type == typeof(short))
        {
            writer.WriteNumber(propertyName, (short)(object)value);
        }
        else if (type == typeof(int))
        {
            writer.WriteNumber(propertyName, (int)(object)value);
        }
        else if (type == typeof(long))
        {
            writer.WriteNumber(propertyName, (long)(object)value);
        }
        else if (type == typeof(double))
        {
            writer.WriteNumber(propertyName, (double)(object)value);
        }
        else if (type == typeof(float))
        {
            writer.WriteNumber(propertyName, (float)(object)value);
        }
        else if (type == typeof(decimal))
        {
            writer.WriteNumber(propertyName, (decimal)(object)value);
        }
        else
        {
            Debug.Fail($"Unhandled Metric Type {typeof(T)}.", "This instruction should be unreachable.");
        }
    }
}
