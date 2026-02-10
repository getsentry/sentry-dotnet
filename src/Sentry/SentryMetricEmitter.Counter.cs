namespace Sentry;

public abstract partial class SentryMetricEmitter
{
    /// <summary>
    /// Increment a counter.
    /// </summary>
    /// <param name="name">The name of the metric.</param>
    /// <param name="value">The value of the metric.</param>
    /// <typeparam name="T">The numeric type of the metric.</typeparam>
    /// <remarks>Supported numeric value types for <typeparamref name="T"/> are <see langword="byte"/>, <see langword="short"/>, <see langword="int"/>, <see langword="long"/>, <see langword="float"/>, and <see langword="double"/>.</remarks>
    public void EmitCounter<T>(string name, T value) where T : struct
    {
        CaptureMetric(SentryMetricType.Counter, name, value, null, [], null);
    }

    /// <summary>
    /// Increment a counter.
    /// </summary>
    /// <param name="name">The name of the metric.</param>
    /// <param name="value">The value of the metric.</param>
    /// <param name="scope">The scope to capture the metric with.</param>
    /// <typeparam name="T">The numeric type of the metric.</typeparam>
    /// <remarks>Supported numeric value types for <typeparamref name="T"/> are <see langword="byte"/>, <see langword="short"/>, <see langword="int"/>, <see langword="long"/>, <see langword="float"/>, and <see langword="double"/>.</remarks>
    public void EmitCounter<T>(string name, T value, Scope? scope) where T : struct
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
    /// <remarks>Supported numeric value types for <typeparamref name="T"/> are <see langword="byte"/>, <see langword="short"/>, <see langword="int"/>, <see langword="long"/>, <see langword="float"/>, and <see langword="double"/>.</remarks>
    public void EmitCounter<T>(string name, T value, IEnumerable<KeyValuePair<string, object>>? attributes, Scope? scope = null) where T : struct
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
    /// <remarks>Supported numeric value types for <typeparamref name="T"/> are <see langword="byte"/>, <see langword="short"/>, <see langword="int"/>, <see langword="long"/>, <see langword="float"/>, and <see langword="double"/>.</remarks>
    public void EmitCounter<T>(string name, T value, ReadOnlySpan<KeyValuePair<string, object>> attributes, Scope? scope = null) where T : struct
    {
        CaptureMetric(SentryMetricType.Counter, name, value, null, attributes, scope);
    }
}
