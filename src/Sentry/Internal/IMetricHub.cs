using Sentry.Protocol.Metrics;

namespace Sentry.Internal;

/// <summary>
/// Specifies various internal methods required on the Hub for metrics to work.
/// </summary>
internal interface IMetricHub : IHub
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
