namespace Sentry;

public abstract partial class SentryTraceMetrics
{
    /// <summary>
    /// Set a gauge value.
    /// </summary>
    /// <param name="name">The name of the metric.</param>
    /// <param name="value">The value of the metric.</param>
    /// <typeparam name="T">The numeric type of the metric.</typeparam>
    /// <remarks>Supported numeric value types for <typeparamref name="T"/> are <see langword="byte"/>, <see langword="short"/>, <see langword="int"/>, <see langword="long"/>, <see langword="float"/>, and <see langword="double"/>.</remarks>
    public void EmitGauge<T>(string name, T value) where T : struct
    {
        CaptureMetric(SentryMetricType.Gauge, name, value, null, [], null);
    }

    /// <summary>
    /// Set a gauge value.
    /// </summary>
    /// <param name="name">The name of the metric.</param>
    /// <param name="value">The value of the metric.</param>
    /// <param name="unit">The unit of measurement.</param>
    /// <typeparam name="T">The numeric type of the metric.</typeparam>
    /// <remarks>Supported numeric value types for <typeparamref name="T"/> are <see langword="byte"/>, <see langword="short"/>, <see langword="int"/>, <see langword="long"/>, <see langword="float"/>, and <see langword="double"/>.</remarks>
    public void EmitGauge<T>(string name, T value, string? unit) where T : struct
    {
        CaptureMetric(SentryMetricType.Gauge, name, value, unit, [], null);
    }

    /// <summary>
    /// Set a gauge value.
    /// </summary>
    /// <param name="name">The name of the metric.</param>
    /// <param name="value">The value of the metric.</param>
    /// <param name="scope">The scope to capture the metric with.</param>
    /// <typeparam name="T">The numeric type of the metric.</typeparam>
    /// <remarks>Supported numeric value types for <typeparamref name="T"/> are <see langword="byte"/>, <see langword="short"/>, <see langword="int"/>, <see langword="long"/>, <see langword="float"/>, and <see langword="double"/>.</remarks>
    public void EmitGauge<T>(string name, T value, Scope? scope) where T : struct
    {
        CaptureMetric(SentryMetricType.Gauge, name, value, null, [], scope);
    }

    /// <summary>
    /// Set a gauge value.
    /// </summary>
    /// <param name="name">The name of the metric.</param>
    /// <param name="value">The value of the metric.</param>
    /// <param name="unit">The unit of measurement.</param>
    /// <param name="scope">The scope to capture the metric with.</param>
    /// <typeparam name="T">The numeric type of the metric.</typeparam>
    /// <remarks>Supported numeric value types for <typeparamref name="T"/> are <see langword="byte"/>, <see langword="short"/>, <see langword="int"/>, <see langword="long"/>, <see langword="float"/>, and <see langword="double"/>.</remarks>
    public void EmitGauge<T>(string name, T value, string? unit, Scope? scope) where T : struct
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
    /// <remarks>Supported numeric value types for <typeparamref name="T"/> are <see langword="byte"/>, <see langword="short"/>, <see langword="int"/>, <see langword="long"/>, <see langword="float"/>, and <see langword="double"/>.</remarks>
    public void EmitGauge<T>(string name, T value, string? unit, IEnumerable<KeyValuePair<string, object>>? attributes, Scope? scope = null) where T : struct
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
    /// <remarks>Supported numeric value types for <typeparamref name="T"/> are <see langword="byte"/>, <see langword="short"/>, <see langword="int"/>, <see langword="long"/>, <see langword="float"/>, and <see langword="double"/>.</remarks>
    public void EmitGauge<T>(string name, T value, string? unit, ReadOnlySpan<KeyValuePair<string, object>> attributes, Scope? scope = null) where T : struct
    {
        CaptureMetric(SentryMetricType.Gauge, name, value, unit, attributes, scope);
    }
}
