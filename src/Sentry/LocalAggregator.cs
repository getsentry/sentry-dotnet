using Sentry.Extensibility;
using Sentry.Protocol.Metrics;

namespace Sentry;

internal class LocalAggregator : ISentryJsonSerializable
{
    private Lazy<ConcurrentDictionary<string, LocalAggregate>> LazyMeasurements { get; } = new();
    internal ConcurrentDictionary<string, LocalAggregate> Measurements => LazyMeasurements.Value;

    public void Add(
        MetricType ty,
        string key,
        double value = 1.0,
        MeasurementUnit? unit = null,
        IDictionary<string, string>? tags = null
        )
    {
        unit ??= MeasurementUnit.None;

        var bucketKey = MetricHelper.GetMetricBucketKey(ty, key, unit.Value, tags);

        Measurements.AddOrUpdate(
            bucketKey,
            _ => new LocalAggregate(ty, key, value, unit.Value, tags),
            (_, metric) =>
            {
                // This prevents multiple threads from trying to mutate the metric at the same time. The only other
                // operation performed against metrics is adding one to the bucket (guaranteed to be atomic due to
                // the use of a ConcurrentDictionary for the timeBucket).
                lock (metric)
                {
                    metric.Add(value);
                }
                return metric;
            });
    }

    public void WriteTo(Utf8JsonWriter writer, IDiagnosticLogger? logger)
    {
        if (Measurements.IsEmpty)
        {
            return;
        }

        writer.WriteStartObject();

        // For the Metrics Summary we group all the metrics by an export key.
        // See https://github.com/getsentry/rfcs/blob/main/text/0123-metrics-correlation.md#basics
        var sortedMeasurements = Measurements.OrderBy(kvp => kvp.Key);
        string? lastKey = null;
        foreach (var (_, value) in sortedMeasurements)
        {
            var exportKey = value.ExportKey;
            if (exportKey != lastKey)
            {
                if (lastKey is not null)
                {
                    writer.WriteEndArray();
                }
                writer.WritePropertyName(exportKey);
                writer.WriteStartArray();
                lastKey = exportKey;
            }
            value.WriteTo(writer, logger);
        }
        writer.WriteEndArray();

        writer.WriteEndObject();
    }
}
