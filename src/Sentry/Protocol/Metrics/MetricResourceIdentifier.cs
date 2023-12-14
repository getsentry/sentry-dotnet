namespace Sentry.Protocol.Metrics;

internal record struct MetricResourceIdentifier(MetricType MetricType, string Key, MeasurementUnit Unit)
{
    public override string ToString() => $"{MetricType}:{MetricHelper.SanitizeKey(Key)}@{Unit}";
}
