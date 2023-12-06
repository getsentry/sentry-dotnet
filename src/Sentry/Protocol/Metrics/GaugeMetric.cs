using Sentry.Extensibility;

namespace Sentry.Protocol.Metrics;

/// <summary>
/// Gauges track a value that can go up and down.
/// </summary>
internal class GaugeMetric : Metric
{
    public GaugeMetric()
    {
        Value = 0;
        First = 0;
        Min = 0;
        Max = 0;
        Sum = 0;
        Count = 0;
    }

    public GaugeMetric(string key, double value, MeasurementUnit? unit = null, IDictionary<string, string>? tags = null,
        DateTime? timestamp = null)
        : base(key, unit, tags, timestamp)
    {
        Value = value;
        First = value;
        Min = value;
        Max = value;
        Sum = value;
        Count = 1;
    }

    public double Value { get; private set; }
    public double First { get; private set; }
    public double Min { get; private set; }
    public double Max { get; private set; }
    public double Sum { get; private set; }
    public double Count { get; private set; }

    protected override string MetricType => "g";

    public override void Add(double value)
    {
        Value = value;
        Min = Math.Min(Min, value);
        Max = Math.Max(Max, value);
        Sum += value;
        Count++;
    }

    protected override void WriteConcreteProperties(Utf8JsonWriter writer, IDiagnosticLogger? logger)
    {
        writer.WriteNumber("value", Value);
        writer.WriteNumber("first", First);
        writer.WriteNumber("min", Min);
        writer.WriteNumber("max", Max);
        writer.WriteNumber("sum", Sum);
        writer.WriteNumber("count", Count);
    }
}
