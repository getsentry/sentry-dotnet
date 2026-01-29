using Sentry.Infrastructure;
using Sentry.Internal;

namespace Sentry;

public abstract partial class SentryMetric
{
    internal static SentryMetric<T> Create<T>(IHub hub, SentryOptions options, ISystemClock clock, SentryMetricType type, string name, T value, string? unit, IEnumerable<KeyValuePair<string, object>>? attributes, Scope? scope) where T : struct
    {
        Debug.Assert(IsSupported<T>());
        Debug.Assert(!string.IsNullOrEmpty(name));
        Debug.Assert(type is not SentryMetricType.Counter || unit is null, $"'{nameof(unit)}' is only used for Metrics of type {nameof(SentryMetricType.Gauge)} and {nameof(SentryMetricType.Distribution)}.");

        var timestamp = clock.GetUtcNow();
        GetTraceIdAndSpanId(hub, out var traceId, out var spanId);

        var metric = new SentryMetric<T>(timestamp, traceId, type, name, value)
        {
            SpanId = spanId,
            Unit = unit,
        };

        scope ??= hub.GetScope();
        metric.SetDefaultAttributes(options, scope?.Sdk ?? SdkVersion.Instance);
        metric.Apply(scope);

        metric.SetAttributes(attributes);

        return metric;
    }

    internal static SentryMetric<T> Create<T>(IHub hub, SentryOptions options, ISystemClock clock, SentryMetricType type, string name, T value, string? unit, ReadOnlySpan<KeyValuePair<string, object>> attributes, Scope? scope) where T : struct
    {
        Debug.Assert(IsSupported<T>());
        Debug.Assert(!string.IsNullOrEmpty(name));
        Debug.Assert(type is not SentryMetricType.Counter || unit is null, $"'{nameof(unit)}' is only used for Metrics of type {nameof(SentryMetricType.Gauge)} and {nameof(SentryMetricType.Distribution)}.");

        var timestamp = clock.GetUtcNow();
        GetTraceIdAndSpanId(hub, out var traceId, out var spanId);

        var metric = new SentryMetric<T>(timestamp, traceId, type, name, value)
        {
            SpanId = spanId,
            Unit = unit,
        };

        scope ??= hub.GetScope();
        metric.SetDefaultAttributes(options, scope?.Sdk ?? SdkVersion.Instance);
        metric.Apply(scope);

        metric.SetAttributes(attributes);

        return metric;
    }

    internal static void GetTraceIdAndSpanId(IHub hub, out SentryId traceId, out SpanId? spanId)
    {
        var activeSpan = hub.GetSpan();
        if (activeSpan is not null)
        {
            traceId = activeSpan.TraceId;
            spanId = activeSpan.SpanId;
            return;
        }

        // set "span_id" to the ID of the Span that was active when the Metric was emitted
        // do not set "span_id" if there was no active Span
        spanId = null;

        var scope = hub.GetScope();
        if (scope is not null)
        {
            traceId = scope.PropagationContext.TraceId;
            return;
        }

        Debug.Assert(hub is not Hub, "In case of a 'full' Hub, there is always a Scope. Otherwise (disabled) there is no Scope, but this branch should be unreachable.");
        traceId = SentryId.Empty;
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
