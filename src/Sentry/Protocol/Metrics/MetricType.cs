namespace Sentry.Protocol.Metrics;

internal enum MetricType : byte { Counter, Gauge, Distribution, Set }

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
