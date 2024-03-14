using Sentry.Extensibility;

namespace Sentry.Protocol.Metrics;

internal class MetricsSummary : ISentryJsonSerializable
{
    private readonly IDictionary<string, List<SpanMetric>> _measurements;

    public MetricsSummary(MetricsSummaryAggregator aggregator)
    {
        // For the Metrics Summary we group all the metrics by an export key.
        // See https://github.com/getsentry/rfcs/blob/main/text/0123-metrics-correlation.md#basics
        var measurements = new Dictionary<string, List<SpanMetric>>();
        foreach (var (_, value) in aggregator.Measurements)
        {
            var exportKey = value.ExportKey;
#if NET6_0_OR_GREATER
            measurements.TryAdd(exportKey, new List<SpanMetric>());
#else
            if (!measurements.ContainsKey(exportKey))
            {
                measurements.Add(exportKey, new List<SpanMetric>());
            }
#endif
            measurements[exportKey].Add(value);
        }
        _measurements = measurements.ToImmutableSortedDictionary();
    }

    public void WriteTo(Utf8JsonWriter writer, IDiagnosticLogger? logger)
    {
        writer.WriteStartObject();

        foreach (var (exportKey, value) in _measurements)
        {
            writer.WritePropertyName(exportKey);
            writer.WriteStartArray();
            foreach (var metric in value.OrderBy(x => MetricHelper.GetMetricBucketKey(x.MetricType, x.Key, x.Unit, x.Tags)))
            {
                metric.WriteTo(writer, logger);
            }
            writer.WriteEndArray();
        }

        writer.WriteEndObject();
    }
}
