using Sentry.Extensibility;
using Sentry.Internal.Extensions;
using ISentrySerializable = Sentry.Protocol.Envelopes.ISerializable;

namespace Sentry.Protocol.Metrics;

internal abstract class Metric : IJsonSerializable, ISentrySerializable
{
    protected Metric() : this(string.Empty)
    {
    }

    protected Metric(string key, MeasurementUnit? unit = null, IDictionary<string, string>? tags = null, DateTime? timestamp = null)
    {
        Key = key;
        Unit = unit;
        Tags = tags ?? new Dictionary<string, string>();
        Timestamp = timestamp ?? DateTime.UtcNow;
    }

    public SentryId EventId { get; private set; } = SentryId.Create();

    public string Key { get; private set; }

    public DateTime Timestamp { get; private set; }

    public MeasurementUnit? Unit { get; private set; }

    public IDictionary<string, string> Tags { get; private set; }

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
        writer.WriteStringDictionaryIfNotEmpty("tags", Tags!);
        WriteValues(writer, logger);
        writer.WriteEndObject();
    }

    protected abstract IEnumerable<IConvertible> SerializedStatsdValues();

    internal static string SanitizeKey(string input) => Regex.Replace(input, @"[^a-zA-Z0-9_/.-]+", "_");
    internal static string SanitizeValue(string input) => Regex.Replace(input, @"[^\w\d_:/@\.\{\}\[\]$-]+", "_");

    public async Task SerializeAsync(Stream stream, IDiagnosticLogger? logger, CancellationToken cancellationToken = default)
    {
        /*
         * We're serializing using the statsd format here: https://github.com/b/statsd_spec
         */
        var metricName = SanitizeKey(Key);
        await Write(metricName).ConfigureAwait(false);
        await Write("@").ConfigureAwait(false);
        var unit = Unit ?? MeasurementUnit.None;
        await Write(unit.ToString()).ConfigureAwait(false);

        foreach (var value in SerializedStatsdValues())
        {
            await Write(":").ConfigureAwait(false);
            await Write(value.ToString(CultureInfo.InvariantCulture)).ConfigureAwait(false);
        }

        await Write("|").ConfigureAwait(false);
        await Write(StatsdType).ConfigureAwait(false);

        if (Tags is { Count: > 0 } tags)
        {
            await Write("|#").ConfigureAwait(false);
            var first = true;
            foreach (var (key, value) in tags)
            {
                var tagKey = SanitizeKey(key);
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
                    await Write(",").ConfigureAwait(false);
                }
                await Write(key).ConfigureAwait(false);
                await Write(":").ConfigureAwait(false);
                await Write(SanitizeValue(value)).ConfigureAwait(false);
            }
        }

        await Write("|T").ConfigureAwait(false);
        await Write(Timestamp.GetTimeBucketKey().ToString(CultureInfo.InvariantCulture)).ConfigureAwait(false);
        await Write("\n").ConfigureAwait(false);
        return;

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
