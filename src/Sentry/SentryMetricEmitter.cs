using Sentry.Infrastructure;
using Sentry.Internal;

namespace Sentry;

/// <summary>
/// Creates and sends metrics to Sentry.
/// </summary>
public abstract partial class SentryMetricEmitter
{
    internal static SentryMetricEmitter Create(IHub hub, SentryOptions options, ISystemClock clock)
        => Create(hub, options, clock, 100, TimeSpan.FromSeconds(5));

    internal static SentryMetricEmitter Create(IHub hub, SentryOptions options, ISystemClock clock, int batchCount, TimeSpan batchInterval)
    {
        return options.Experimental.EnableMetrics
            ? new DefaultSentryMetricEmitter(hub, options, clock, batchCount, batchInterval)
            : DisabledSentryMetricEmitter.Instance;
    }

    private protected SentryMetricEmitter()
    {
    }

    /// <summary>
    /// Buffers a <see href="https://develop.sentry.dev/sdk/telemetry/metrics/">Sentry Metric</see> item
    /// via the associated <see href="https://develop.sentry.dev/sdk/telemetry/telemetry-processor/batch-processor/">Batch Processor</see>.
    /// </summary>
    /// <param name="type">The type of metric.</param>
    /// <param name="name">The name of the metric.</param>
    /// <param name="value">The numeric value of the metric.</param>
    /// <param name="unit">The unit of measurement for the metric value.</param>
    /// <param name="attributes">A dictionary of key-value pairs of arbitrary data attached to the metric.</param>
    /// <param name="scope">The optional scope to capture the metric with.</param>
    /// <typeparam name="T">The numeric type of the metric.</typeparam>
    private protected abstract void CaptureMetric<T>(SentryMetricType type, string name, T value, string? unit, IEnumerable<KeyValuePair<string, object>>? attributes, Scope? scope) where T : struct;

    /// <summary>
    /// Buffers a <see href="https://develop.sentry.dev/sdk/telemetry/metrics/">Sentry Metric</see> item
    /// via the associated <see href="https://develop.sentry.dev/sdk/telemetry/telemetry-processor/batch-processor/">Batch Processor</see>.
    /// </summary>
    /// <param name="type">The type of metric.</param>
    /// <param name="name">The name of the metric.</param>
    /// <param name="value">The numeric value of the metric.</param>
    /// <param name="unit">The unit of measurement for the metric value.</param>
    /// <param name="attributes">A dictionary of key-value pairs of arbitrary data attached to the metric.</param>
    /// <param name="scope">The optional scope to capture the metric with.</param>
    /// <typeparam name="T">The numeric type of the metric.</typeparam>
    private protected abstract void CaptureMetric<T>(SentryMetricType type, string name, T value, string? unit, ReadOnlySpan<KeyValuePair<string, object>> attributes, Scope? scope) where T : struct;

    /// <summary>
    /// Buffers a <see href="https://develop.sentry.dev/sdk/telemetry/metrics/">Sentry Metric</see> item
    /// via the associated <see href="https://develop.sentry.dev/sdk/telemetry/telemetry-processor/batch-processor/">Batch Processor</see>.
    /// </summary>
    /// <param name="metric">The metric.</param>
    /// <typeparam name="T">The numeric type of the metric.</typeparam>
    private protected abstract void CaptureMetric<T>(SentryMetric<T> metric) where T : struct;

    /// <summary>
    /// Clears all buffers for this metrics and causes any buffered metrics to be sent by the underlying <see cref="ISentryClient"/>.
    /// </summary>
    protected internal abstract void Flush();
}
