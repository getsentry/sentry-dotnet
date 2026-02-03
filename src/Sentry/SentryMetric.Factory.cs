using Sentry.Infrastructure;

namespace Sentry;

public abstract partial class SentryMetric
{
    internal static SentryMetric<T> Create<T>(IHub hub, SentryOptions options, ISystemClock clock, SentryMetricType type, string name, T value, string? unit, IEnumerable<KeyValuePair<string, object>>? attributes, Scope? scope) where T : struct
    {
        Debug.Assert(IsSupported<T>());
        Debug.Assert(!string.IsNullOrEmpty(name));
        Debug.Assert(type is not SentryMetricType.Counter || unit is null, $"'{nameof(unit)}' is only used for Metrics of type {nameof(SentryMetricType.Gauge)} and {nameof(SentryMetricType.Distribution)}.");

        var timestamp = clock.GetUtcNow();
        hub.GetTraceIdAndSpanId(out var traceId, out var spanId);

        var metric = new SentryMetric<T>(timestamp, traceId, type, name, value)
        {
            SpanId = spanId,
            Unit = unit,
        };

        scope ??= hub.GetScope();
        metric.SetDefaultAttributes(options, scope?.Sdk ?? SdkVersion.Instance);

        metric.SetAttributes(attributes);

        return metric;
    }

    internal static SentryMetric<T> Create<T>(IHub hub, SentryOptions options, ISystemClock clock, SentryMetricType type, string name, T value, string? unit, ReadOnlySpan<KeyValuePair<string, object>> attributes, Scope? scope) where T : struct
    {
        Debug.Assert(IsSupported<T>());
        Debug.Assert(!string.IsNullOrEmpty(name));
        Debug.Assert(type is not SentryMetricType.Counter || unit is null, $"'{nameof(unit)}' is only used for Metrics of type {nameof(SentryMetricType.Gauge)} and {nameof(SentryMetricType.Distribution)}.");

        var timestamp = clock.GetUtcNow();
        hub.GetTraceIdAndSpanId(out var traceId, out var spanId);

        var metric = new SentryMetric<T>(timestamp, traceId, type, name, value)
        {
            SpanId = spanId,
            Unit = unit,
        };

        scope ??= hub.GetScope();
        metric.SetDefaultAttributes(options, scope?.Sdk ?? SdkVersion.Instance);

        metric.SetAttributes(attributes);

        return metric;
    }

    private static bool IsSupported<T>() where T : struct
    {
        var valueType = typeof(T);
        return IsSupported(valueType);
    }

    internal static bool IsSupported(Type valueType)
    {
        return valueType == typeof(long) || valueType == typeof(double)
            || valueType == typeof(int) || valueType == typeof(float)
            || valueType == typeof(short) || valueType == typeof(byte);
    }
}
