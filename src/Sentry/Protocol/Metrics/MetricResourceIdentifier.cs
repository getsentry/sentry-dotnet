namespace Sentry.Protocol.Metrics;

/// <summary>
/// Uniquely identifies a metric resource.
/// </summary>
/// <param name="MetricType"></param>
/// <param name="Key"></param>
/// <param name="Unit"></param>
internal record struct MetricResourceIdentifier(MetricType MetricType, string Key, MeasurementUnit Unit)
{
    /// <summary>
    /// Returns a string representation of the metric resource identifier.
    /// </summary>
    public override string ToString()
        => $"{MetricType.ToStatsdType()}:{MetricHelper.SanitizeTagKey(Key)}@{Unit}";
}
