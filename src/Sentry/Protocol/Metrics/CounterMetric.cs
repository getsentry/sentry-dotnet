using Sentry.Extensibility;

namespace Sentry.Protocol.Metrics;

/// <summary>
/// Counters track a value that can only be incremented.
/// </summary>
internal class CounterMetric : Metric
{
    public int Value { get; set; }

    public override void WriteConcreteProperties(Utf8JsonWriter writer, IDiagnosticLogger? logger) =>
        writer.WriteNumber("value", Value);
}
