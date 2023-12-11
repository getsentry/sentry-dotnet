namespace Sentry;

/// <summary>
/// Exposes EXPERIMENTAL capability to emit metrics. This API is subject to change without major version bumps so use
/// with caution. We advise disabling in production at the moment.
/// </summary>
public interface IMetricAggregator
{
    /// <summary>
    /// Emits a Counter metric
    /// </summary>
    /// <param name="key">A unique key identifying the metric</param>
    /// <param name="value">The value to be added</param>
    /// <param name="unit">An optional <see cref="MeasurementUnit"/></param>
    /// <param name="tags">Optional Tags to associate with the metric</param>
    /// <param name="timestamp">
    /// The time when the metric was emitted. Defaults to the time at which the metric is emitted, if no value is provided.
    /// </param>
    void Increment(
        string key,
        double value = 1.0,
        MeasurementUnit? unit = null,
        IDictionary<string, string>? tags = null,
        DateTime? timestamp = null
        // , int stacklevel = 0 // Used for code locations
    );

    /// <summary>
    /// Emits a Gauge metric
    /// </summary>
    /// <param name="key">A unique key identifying the metric</param>
    /// <param name="value">The value to be added</param>
    /// <param name="unit">An optional <see cref="MeasurementUnit"/></param>
    /// <param name="tags">Optional Tags to associate with the metric</param>
    /// <param name="timestamp">
    /// The time when the metric was emitted. Defaults to the time at which the metric is emitted, if no value is provided.
    /// </param>
    void Gauge(
        string key,
        double value = 1.0,
        MeasurementUnit? unit = null,
        IDictionary<string, string>? tags = null,
        DateTime? timestamp = null
        // , int stacklevel = 0 // Used for code locations
    );

    /// <summary>
    /// Emits a Distribution metric
    /// </summary>
    /// <param name="key">A unique key identifying the metric</param>
    /// <param name="value">The value to be added</param>
    /// <param name="unit">An optional <see cref="MeasurementUnit"/></param>
    /// <param name="tags">Optional Tags to associate with the metric</param>
    /// <param name="timestamp">
    /// The time when the metric was emitted. Defaults to the time at which the metric is emitted, if no value is provided.
    /// </param>
    void Distribution(
        string key,
        double value = 1.0,
        MeasurementUnit? unit = null,
        IDictionary<string, string>? tags = null,
        DateTime? timestamp = null
        // , int stacklevel = 0 // Used for code locations
    );

    /// <summary>
    /// Emits a Set metric
    /// </summary>
    /// <param name="key">A unique key identifying the metric</param>
    /// <param name="value">The value to be added</param>
    /// <param name="unit">An optional <see cref="MeasurementUnit"/></param>
    /// <param name="tags">Optional Tags to associate with the metric</param>
    /// <param name="timestamp">
    /// The time when the metric was emitted. Defaults to the time at which the metric is emitted, if no value is provided.
    /// </param>
    void Set(
        string key,
        double value = 1.0,
        MeasurementUnit? unit = null,
        IDictionary<string, string>? tags = null,
        DateTime? timestamp = null
        // , int stacklevel = 0 // Used for code locations
    );

    /// <summary>
    /// Emits a distribution with the time it takes to run a given code block.
    /// </summary>
    /// <param name="key">A unique key identifying the metric</param>
    /// <param name="value">The value to be added</param>
    /// <param name="unit">
    /// An optional <see cref="MeasurementUnit.Duration"/>. Defaults to <see cref="MeasurementUnit.Duration.Second"/>
    /// </param>
    /// <param name="tags">Optional Tags to associate with the metric</param>
    /// <param name="timestamp">The time when the metric was emitted</param>
    void Timing(
        string key,
        double value,
        MeasurementUnit.Duration unit = MeasurementUnit.Duration.Second,
        IDictionary<string, string>? tags = null,
        DateTime? timestamp = null
        // , int stacklevel = 0 // Used for code locations
    );
}
