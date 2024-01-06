using Sentry.Extensibility;
using Sentry.Internal.Extensions;
using ISentrySerializable = Sentry.Protocol.Envelopes.ISerializable;

namespace Sentry.Protocol.Metrics;

internal abstract class Metric : IJsonSerializable, ISentrySerializable
{
    protected Metric() : this(string.Empty)
    {
    }

    protected Metric(string key, MeasurementUnit? unit = null, IDictionary<string, string>? tags = null, DateTimeOffset? timestamp = null)
    {
        Key = key;
        Unit = unit;
        _tags = tags;
        Timestamp = timestamp ?? DateTimeOffset.UtcNow;
    }

    public SentryId EventId { get; private set; } = SentryId.Create();

    public string Key { get; private set; }

    public DateTimeOffset Timestamp { get; private set; }

    public MeasurementUnit? Unit { get; private set; }

    private IDictionary<string, string>? _tags;

    public IDictionary<string, string> Tags
    {
        get
        {
            _tags ??= new Dictionary<string, string>();
            return _tags;
        }
    }

    public abstract void Add(double value);

    protected abstract void WriteValues(Utf8JsonWriter writer, IDiagnosticLogger? logger);

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

    protected abstract IEnumerable<IConvertible> SerializedStatsdValues();

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
            await Write($":{value.ToString(CultureInfo.InvariantCulture)}");
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
