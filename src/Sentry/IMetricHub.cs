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

    /// <summary>
    /// Starts a child span for the current transaction or, if there is no active transaction, starts a new transaction.
    /// </summary>
    ISpan StartSpan(string name, string operation, string description);

    /// <inheritdoc cref="IHub.GetSpan"/>
    ISpan? GetSpan();
}

internal static class MetricHubExtensions
{
    public static ISpan StartSpan(this IMetricHub hub, string operation, string description) =>
        hub.StartSpan(description, operation, description);
}
