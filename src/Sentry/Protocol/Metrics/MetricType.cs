namespace Sentry.Protocol.Metrics;

/// <summary>
/// The metric instrument type
/// </summary>
internal enum MetricType : byte
{
    /// <inheritdoc cref="CounterMetric"/>
    Counter,
    /// <inheritdoc cref="GaugeMetric"/>
    Gauge,
    /// <inheritdoc cref="DistributionMetric"/>
    Distribution,
    /// <inheritdoc cref="SetMetric"/>
    Set
}

internal static class MetricTypeExtensions
{
    internal static string ToStatsdType(this MetricType type) =>
        type switch
        {
            MetricType.Counter => "c",
            MetricType.Gauge => "g",
            MetricType.Distribution => "d",
            MetricType.Set => "s",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
}
