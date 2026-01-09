using Sentry.Infrastructure;

namespace Sentry;

internal static class SentryMetric
{
    internal static SentryMetric<T> Create<T>(IHub hub, ISystemClock clock, SentryMetricType type, string name, T value, string? unit, IEnumerable<KeyValuePair<string, object>>? attributes, Scope? scope) where T : struct
    {
        Debug.Assert(type is not SentryMetricType.Counter || unit is null, $"'{nameof(unit)}' is only used for Metrics of type {nameof(SentryMetricType.Gauge)} and {nameof(SentryMetricType.Distribution)}.");

        var timestamp = clock.GetUtcNow();
        SentryLog.GetTraceIdAndSpanId(hub, out var traceId, out var spanId); //TODO: move

        var metric = new SentryMetric<T>(timestamp, traceId, type, name, value)
        {
            SpanId = spanId,
            Unit = unit,
        };

        scope ??= hub.GetScope();
        metric.Apply(scope);

        metric.SetAttributes(attributes);

        return metric;
    }

    internal static SentryMetric<T> Create<T>(IHub hub, ISystemClock clock, SentryMetricType type, string name, T value, string? unit, ReadOnlySpan<KeyValuePair<string, object>> attributes, Scope? scope) where T : struct
    {
        Debug.Assert(type is not SentryMetricType.Counter || unit is null, $"'{nameof(unit)}' is only used for Metrics of type {nameof(SentryMetricType.Gauge)} and {nameof(SentryMetricType.Distribution)}.");

        var timestamp = clock.GetUtcNow();
        SentryLog.GetTraceIdAndSpanId(hub, out var traceId, out var spanId);

        var metric = new SentryMetric<T>(timestamp, traceId, type, name, value)
        {
            SpanId = spanId,
            Unit = unit,
        };

        scope ??= hub.GetScope();
        metric.Apply(scope);

        metric.SetAttributes(attributes);

        return metric;
    }
}
