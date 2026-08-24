using Sentry.Extensibility;

namespace Sentry;

/// <summary>
/// The type of metric.
/// </summary>
public enum SentryMetricType
{
    /// <summary>
    /// A metric that increments counts.
    /// </summary>
    /// <remarks>
    /// <see cref="SentryMetric{T}._value"/> represents the count to increment by.
    /// By default: <see langword="1"/>.
    /// </remarks>
    Counter,

    /// <summary>
    /// A metric that tracks a value that can go up or down.
    /// </summary>
    /// <remarks>
    /// <see cref="SentryMetric{T}._value"/> represents the current value.
    /// </remarks>
    Gauge,

    /// <summary>
    /// A metric that tracks the statistical distribution of values.
    /// </summary>
    /// <remarks>
    /// <see cref="SentryMetric{T}._value"/> represents a single measured value.
    /// </remarks>
    Distribution,
}

internal static class SentryMetricTypeExtensions
{
    internal static string ToProtocolString(this SentryMetricType type, IDiagnosticLogger? logger)
    {
        return type switch
        {
            SentryMetricType.Counter => "counter",
            SentryMetricType.Gauge => "gauge",
            SentryMetricType.Distribution => "distribution",
            _ => IsNotDefined(type, logger),
        };

        static string IsNotDefined(SentryMetricType type, IDiagnosticLogger? logger)
        {
            logger?.LogDebug("Metric type {0} is not defined.", type);
            return "unknown";
        }
    }
}
