using Sentry.Infrastructure;

namespace Sentry;

public abstract partial class SentryMetric
{
    private static SentryMetric<T> CreateCore<T>(IHub hub, SentryOptions options, ISystemClock clock, SentryMetricType type, string name, T value, string? unit, Scope? scope) where T : struct
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
        metric.Attributes.SetDefaultAttributes(options, scope?.Sdk ?? SdkVersion.Instance);

        return metric;
    }

    internal static SentryMetric<T> Create<T>(IHub hub, SentryOptions options, ISystemClock clock, SentryMetricType type, string name, T value, string? unit, IEnumerable<KeyValuePair<string, object>>? attributes, Scope? scope) where T : struct
    {
        var metric = CreateCore<T>(hub, options, clock, type, name, value, unit, scope);
        metric.Attributes.SetAttributes(attributes);
        return metric;
    }

    internal static SentryMetric<T> Create<T>(IHub hub, SentryOptions options, ISystemClock clock, SentryMetricType type, string name, T value, string? unit, ReadOnlySpan<KeyValuePair<string, object>> attributes, Scope? scope) where T : struct
    {
        var metric = CreateCore<T>(hub, options, clock, type, name, value, unit, scope);
        metric.Attributes.SetAttributes(attributes);
        return metric;
    }

#if NET6_0_OR_GREATER
    [SuppressMessage("Roslynator", "RCS1242:Do not pass non-read-only struct by read-only reference", Justification = $"Ensure that only readonly instance members of {nameof(TagList)} are invoked, to avoid a defensive copy created by the compiler.")]
    internal static SentryMetric<T> Create<T>(IHub hub, SentryOptions options, ISystemClock clock, SentryMetricType type, string name, T value, string? unit, in TagList attributes, Scope? scope) where T : struct
    {
        var metric = CreateCore<T>(hub, options, clock, type, name, value, unit, scope);
        metric.Attributes.SetAttributes(in attributes);
        return metric;
    }
#endif

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
