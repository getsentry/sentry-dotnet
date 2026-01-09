namespace Sentry;

public abstract partial class SentryTraceMetrics
{
    /// <summary>
    /// Increment a counter.
    /// </summary>
    /// <param name="name">The name of the metric.</param>
    /// <param name="value">The value of the metric.</param>
    /// <param name="scope">The scope to capture the metric with.</param>
    /// <typeparam name="T">The numeric type of the metric.</typeparam>
    public void AddCounter<T>(string name, T value, Scope? scope = null) where T : struct
    {
        CaptureMetric(SentryMetricType.Counter, name, value, null, [], scope);
    }

    /// <summary>
    /// Increment a counter.
    /// </summary>
    /// <param name="name">The name of the metric.</param>
    /// <param name="value">The value of the metric.</param>
    /// <param name="attributes">A dictionary of attributes (key-value pairs with type information).</param>
    /// <param name="scope">The scope to capture the metric with.</param>
    /// <typeparam name="T">The numeric type of the metric.</typeparam>
    public void AddCounter<T>(string name, T value, IEnumerable<KeyValuePair<string, object>>? attributes = null, Scope? scope = null) where T : struct
    {
        CaptureMetric(SentryMetricType.Counter, name, value, null, attributes, scope);
    }

    /// <summary>
    /// Increment a counter.
    /// </summary>
    /// <param name="name">The name of the metric.</param>
    /// <param name="value">The value of the metric.</param>
    /// <param name="attributes">A dictionary of attributes (key-value pairs with type information).</param>
    /// <param name="scope">The scope to capture the metric with.</param>
    /// <typeparam name="T">The numeric type of the metric.</typeparam>
    public void AddCounter<T>(string name, T value, ReadOnlySpan<KeyValuePair<string, object>> attributes, Scope? scope = null) where T : struct
    {
        CaptureMetric(SentryMetricType.Counter, name, value, null, attributes, scope);
    }

    /// <summary>
    /// Set a gauge value.
    /// </summary>
    /// <param name="name">The name of the metric.</param>
    /// <param name="value">The value of the metric.</param>
    /// <param name="unit">The unit of measurement.</param>
    /// <param name="scope">The scope to capture the metric with.</param>
    /// <typeparam name="T">The numeric type of the metric.</typeparam>
    public void RecordGauge<T>(string name, T value, string? unit = null, Scope? scope = null) where T : struct
    {
        CaptureMetric(SentryMetricType.Gauge, name, value, unit, [], scope);
    }

    /// <summary>
    /// Set a gauge value.
    /// </summary>
    /// <param name="name">The name of the metric.</param>
    /// <param name="value">The value of the metric.</param>
    /// <param name="unit">The unit of measurement.</param>
    /// <param name="attributes">A dictionary of attributes (key-value pairs with type information).</param>
    /// <param name="scope">The scope to capture the metric with.</param>
    /// <typeparam name="T">The numeric type of the metric.</typeparam>
    public void RecordGauge<T>(string name, T value, string? unit = null, IEnumerable<KeyValuePair<string, object>>? attributes = null, Scope? scope = null) where T : struct
    {
        CaptureMetric(SentryMetricType.Gauge, name, value, unit, attributes, scope);
    }

    /// <summary>
    /// Set a gauge value.
    /// </summary>
    /// <param name="name">The name of the metric.</param>
    /// <param name="value">The value of the metric.</param>
    /// <param name="unit">The unit of measurement.</param>
    /// <param name="attributes">A dictionary of attributes (key-value pairs with type information).</param>
    /// <param name="scope">The scope to capture the metric with.</param>
    /// <typeparam name="T">The numeric type of the metric.</typeparam>
    public void RecordGauge<T>(string name, T value, string? unit, ReadOnlySpan<KeyValuePair<string, object>> attributes, Scope? scope = null) where T : struct
    {
        CaptureMetric(SentryMetricType.Gauge, name, value, unit, attributes, scope);
    }

    /// <summary>
    /// Add a distribution value.
    /// </summary>
    /// <param name="name">The name of the metric.</param>
    /// <param name="value">The value of the metric.</param>
    /// <param name="unit">The unit of measurement.</param>
    /// <param name="scope">The scope to capture the metric with.</param>
    /// <typeparam name="T">The numeric type of the metric.</typeparam>
    public void RecordDistribution<T>(string name, T value, string? unit = null, Scope? scope = null) where T : struct
    {
        CaptureMetric(SentryMetricType.Distribution, name, value, unit, [], scope);
    }

    /// <summary>
    /// Add a distribution value.
    /// </summary>
    /// <param name="name">The name of the metric.</param>
    /// <param name="value">The value of the metric.</param>
    /// <param name="unit">The unit of measurement.</param>
    /// <param name="attributes">A dictionary of attributes (key-value pairs with type information).</param>
    /// <param name="scope">The scope to capture the metric with.</param>
    /// <typeparam name="T">The numeric type of the metric.</typeparam>
    public void RecordDistribution<T>(string name, T value, string? unit = null, IEnumerable<KeyValuePair<string, object>>? attributes = null, Scope? scope = null) where T : struct
    {
        CaptureMetric(SentryMetricType.Distribution, name, value, unit, attributes, scope);
    }

    /// <summary>
    /// Add a distribution value.
    /// </summary>
    /// <param name="name">The name of the metric.</param>
    /// <param name="value">The value of the metric.</param>
    /// <param name="unit">The unit of measurement.</param>
    /// <param name="attributes">A dictionary of attributes (key-value pairs with type information).</param>
    /// <param name="scope">The scope to capture the metric with.</param>
    /// <typeparam name="T">The numeric type of the metric.</typeparam>
    public void RecordDistribution<T>(string name, T value, string? unit, ReadOnlySpan<KeyValuePair<string, object>> attributes, Scope? scope = null) where T : struct
    {
        CaptureMetric(SentryMetricType.Distribution, name, value, unit, attributes, scope);
    }
}
