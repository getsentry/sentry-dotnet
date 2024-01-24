using Sentry.Extensibility;
using Sentry.Internal.Extensions;
using ISentrySerializable = Sentry.Protocol.Envelopes.ISerializable;

namespace Sentry.Protocol.Metrics;

/// <summary>
/// Base class for metric instruments
/// </summary>
public abstract class Metric : IJsonSerializable, ISentrySerializable
{
    /// <summary>
    /// Creates a new instance of <see cref="Metric"/>.
    /// </summary>
    protected Metric() : this(string.Empty)
    {
    }

    /// <summary>
    /// Creates a new instance of <see cref="Metric"/>.
    /// </summary>
    /// <param name="key">The text key to be used to identify the metric</param>
    /// <param name="unit">An optional <see cref="MeasurementUnit"/> that describes the values being tracked</param>
    /// <param name="tags">An optional set of key/value paris that can be used to add dimensionality to metrics</param>
    /// <param name="timestamp">An optional time when the metric was emitted. Defaults to DateTimeOffset.UtcNow</param>
    protected Metric(string key, MeasurementUnit? unit = null, IDictionary<string, string>? tags = null, DateTimeOffset? timestamp = null)
    {
        Key = key;
        Unit = unit;
        _tags = tags;
        Timestamp = timestamp ?? DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// <see cref="SentryEvent.EventId"/>
    /// </summary>
    public SentryId EventId { get; } = SentryId.Create();

    /// <summary>
    /// A text key identifying the metric
    /// </summary>
    public string Key { get; }

    /// <summary>
    /// The time when the metric was emitted.
    /// </summary>
    public DateTimeOffset Timestamp { get; }

    /// <summary>
    /// A <see cref="MeasurementUnit"/> that describes the values being tracked
    /// </summary>
    public MeasurementUnit? Unit { get; }

    private IDictionary<string, string>? _tags;

    /// <summary>
    /// A set of key/value paris providing dimensionality for the metric
    /// </summary>
    public IDictionary<string, string> Tags
    {
        get
        {
            _tags ??= new Dictionary<string, string>();
            return _tags;
        }
    }

    /// <summary>
    /// Adds a value to the metric
    /// </summary>
    public abstract void Add(double value);

    /// <summary>
    /// Serializes metric values to JSON
    /// </summary>
    protected abstract void WriteValues(Utf8JsonWriter writer, IDiagnosticLogger? logger);

    /// <inheritdoc cref="IJsonSerializable.WriteTo"/>
    public void WriteTo(Utf8JsonWriter writer, IDiagnosticLogger? logger)
    {
        writer.WriteStartObject();
        writer.WriteString("type", GetType().Name);
        writer.WriteSerializable("event_id", EventId, logger);
        writer.WriteString("name", Key);
        writer.WriteString("timestamp", Timestamp);
        if (Unit.HasValue)
        {
            writer.WriteStringIfNotWhiteSpace("unit", Unit.ToString());
        }
        writer.WriteStringDictionaryIfNotEmpty("tags", (IEnumerable<KeyValuePair<string, string?>>?)_tags);
        WriteValues(writer, logger);
        writer.WriteEndObject();
    }

    /// <summary>
    /// Concrete classes should implement this to return a list of values that should be serialized to statsd
    /// </summary>
    protected abstract IEnumerable<object> SerializedStatsdValues();

    /// <summary>
    /// Serializes the metric asynchrounously in statsd format to the provided stream
    /// </summary>
    public async Task SerializeAsync(Stream stream, IDiagnosticLogger? logger, CancellationToken cancellationToken = default)
    {
        /*
         * We're serializing using the statsd format here: https://github.com/b/statsd_spec
         */
        var metricName = MetricHelper.SanitizeKey(Key);
        await Write($"{metricName}@").ConfigureAwait(false);
        var unit = Unit ?? MeasurementUnit.None;
// We don't need ConfigureAwait(false) here as ConfigureAwait on metricName above avoids capturing the ExecutionContext.
#pragma warning disable CA2007
        await Write(unit.ToString());

        foreach (var value in SerializedStatsdValues())
        {
            await Write($":{((IConvertible)value).ToString(CultureInfo.InvariantCulture)}");
        }

        await Write($"|{StatsdType}");

        if (_tags is { Count: > 0 } tags)
        {
            await Write("|#");
            var first = true;
            foreach (var (key, value) in tags)
            {
                var tagKey = MetricHelper.SanitizeKey(key);
                if (string.IsNullOrWhiteSpace(tagKey))
                {
                    continue;
                }
                if (first)
                {
                    first = false;
                }
                else
                {
                    await Write(",");
                }
                await Write($"{key}:SanitizeValue(value)");
            }
        }

        await Write($"|T{Timestamp.GetTimeBucketKey().ToString(CultureInfo.InvariantCulture)}\n");
        return;
#pragma warning restore CA2007

        async Task Write(string content)
        {
            await stream.WriteAsync(Encoding.UTF8.GetBytes(content), cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc cref="ISerializable.Serialize"/>
    public void Serialize(Stream stream, IDiagnosticLogger? logger)
    {
        SerializeAsync(stream, logger).GetAwaiter().GetResult();
    }

    private string StatsdType =>
        this switch
        {
            CounterMetric _ => "c",
            GaugeMetric _ => "g",
            DistributionMetric _ => "d",
            SetMetric _ => "s",
            _ => throw new ArgumentOutOfRangeException(GetType().Name, "Unable to infer statsd type")
        };
}
