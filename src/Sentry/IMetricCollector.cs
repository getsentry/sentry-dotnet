using Sentry.Protocol.Metrics;

namespace Sentry;

internal interface IMetricCollector
{
    void CaptureMetrics(IEnumerable<Metric> metrics);
    void CaptureCodeLocations(CodeLocations codeLocations);
}
