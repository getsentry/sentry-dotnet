namespace Sentry;

/// <summary>
/// Exposes EXPERIMENTAL capability to emit metrics. This API is subject to change without major version bumps so use
/// with caution. We advise disabling in production at the moment.
/// </summary>
public interface IMetricAggregator: IDisposable
{
    /// <summary>
    /// Emits a Counter metric
    /// </summary>
    /// <param name="key">A unique key identifying the metric</param>
    /// <param name="value">The value to be added</param>
    /// <param name="unit">An optional <see cref="MeasurementUnit"/></param>
    /// <param name="tags">Optional Tags to associate with the metric</param>
    /// <param name="timestamp">
    ///     The time when the metric was emitted. Defaults to the time at which the metric is emitted, if no value is provided.
    /// </param>
    /// <param name="stackLevel">Optional number of stacks levels to ignore when determining the code location</param>
    void Increment(string key,
        double value = 1.0,
        MeasurementUnit? unit = null,
        IDictionary<string, string>? tags = null,
        DateTimeOffset? timestamp = null,
        int stackLevel = 1);

    /// <summary>
    /// Emits a Gauge metric
    /// </summary>
    /// <param name="key">A unique key identifying the metric</param>
    /// <param name="value">The value to be added</param>
    /// <param name="unit">An optional <see cref="MeasurementUnit"/></param>
    /// <param name="tags">Optional Tags to associate with the metric</param>
    /// <param name="timestamp">
    ///     The time when the metric was emitted. Defaults to the time at which the metric is emitted, if no value is provided.
    /// </param>
    /// <param name="stackLevel">Optional number of stacks levels to ignore when determining the code location</param>
    void Gauge(string key,
        double value = 1.0,
        MeasurementUnit? unit = null,
        IDictionary<string, string>? tags = null,
        DateTimeOffset? timestamp = null,
        int stackLevel = 1);

    /// <summary>
    /// Emits a Distribution metric
    /// </summary>
    /// <param name="key">A unique key identifying the metric</param>
    /// <param name="value">The value to be added</param>
    /// <param name="unit">An optional <see cref="MeasurementUnit"/></param>
    /// <param name="tags">Optional Tags to associate with the metric</param>
    /// <param name="timestamp">
    ///     The time when the metric was emitted. Defaults to the time at which the metric is emitted, if no value is provided.
    /// </param>
    /// <param name="stackLevel">Optional number of stacks levels to ignore when determining the code location</param>
    void Distribution(string key,
        double value = 1.0,
        MeasurementUnit? unit = null,
        IDictionary<string, string>? tags = null,
        DateTimeOffset? timestamp = null,
        int stackLevel = 1);

    /// <summary>
    /// Emits a Set metric
    /// </summary>
    /// <param name="key">A unique key identifying the metric</param>
    /// <param name="value">The value to be added</param>
    /// <param name="unit">An optional <see cref="MeasurementUnit"/></param>
    /// <param name="tags">Optional Tags to associate with the metric</param>
    /// <param name="timestamp">
    ///     The time when the metric was emitted. Defaults to the time at which the metric is emitted, if no value is provided.
    /// </param>
    /// <param name="stackLevel">Optional number of stacks levels to ignore when determining the code location</param>
    void Set(string key,
        double value = 1.0,
        MeasurementUnit? unit = null,
        IDictionary<string, string>? tags = null,
        DateTimeOffset? timestamp = null,
        int stackLevel = 1);

    /// <summary>
    /// Emits a distribution with the time it takes to run a given code block.
    /// </summary>
    /// <param name="key">A unique key identifying the metric</param>
    /// <param name="value">The value to be added</param>
    /// <param name="unit">
    ///     An optional <see cref="MeasurementUnit.Duration"/>. Defaults to <see cref="MeasurementUnit.Duration.Second"/>
    /// </param>
    /// <param name="tags">Optional Tags to associate with the metric</param>
    /// <param name="timestamp">The time when the metric was emitted</param>
    /// <param name="stackLevel">Optional number of stacks levels to ignore when determining the code location</param>
    void Timing(string key,
        double value,
        MeasurementUnit.Duration unit = MeasurementUnit.Duration.Second,
        IDictionary<string, string>? tags = null,
        DateTimeOffset? timestamp = null,
        int stackLevel = 1);

    /// <summary>
    /// Flushes any flushable metrics and/or code locations.
    /// If <paramref name="force"/> is true then the cutoff is ignored and all metrics are flushed.
    /// </summary>
    /// <param name="force">Forces all buckets to be flushed, ignoring the cutoff</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/></param>
    /// <returns>False if a shutdown is requested during flush, true otherwise</returns>
    Task FlushAsync(bool force = true, CancellationToken cancellationToken = default);
}
