using Sentry.Protocol.Metrics;

namespace Sentry;

internal interface IMetricHub
{
    /// <summary>
    /// Captures one or more metrics to be sent to Sentry.
    /// </summary>
    void CaptureMetrics(IEnumerable<Metric> metrics);

    /// <summary>
    /// Captures one or more <see cref="CodeLocations"/> to be sent to Sentry.
    /// </summary>
    void CaptureCodeLocations(CodeLocations codeLocations);
}
