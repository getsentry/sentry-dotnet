using Sentry.Extensibility;

namespace Sentry.Protocol;

/// <summary>
/// Represents the Sentry protocol for Trace-connected Metrics.
/// </summary>
/// <remarks>
/// Sentry Docs: <see href="https://docs.sentry.io/product/explore/metrics/"/>.
/// Sentry Developer Documentation: <see href="https://develop.sentry.dev/sdk/telemetry/metrics/"/>.
/// </remarks>
internal sealed class TraceMetric : ISentryJsonSerializable
{
    private readonly SentryMetric[] _items;

    public TraceMetric(SentryMetric[] metrics)
    {
        _items = metrics;
    }

    public int Length => _items.Length;
    public ReadOnlySpan<SentryMetric> Items => _items;

    public void WriteTo(Utf8JsonWriter writer, IDiagnosticLogger? logger)
    {
        writer.WriteStartObject();
        writer.WriteStartArray("items");

        foreach (var metric in _items)
        {
            metric.WriteTo(writer, logger);
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
    }
}
