using Sentry.Extensibility;

namespace Sentry;

#if NET6_0_OR_GREATER
/// <summary>
/// Similar to <see cref="System.Diagnostics.Metrics.MeterListener"/>.
/// </summary>
#endif
internal sealed class SentryTraceMetricsCallbacks
{
    private Func<SentryMetric<byte>, SentryMetric<byte>?> _beforeSendMetricByte;
    private Func<SentryMetric<short>, SentryMetric<short>?> _beforeSendMetricInt16;
    private Func<SentryMetric<int>, SentryMetric<int>?> _beforeSendMetricInt32;
    private Func<SentryMetric<long>, SentryMetric<long>?> _beforeSendMetricInt64;
    private Func<SentryMetric<float>, SentryMetric<float>?> _beforeSendMetricSingle;
    private Func<SentryMetric<double>, SentryMetric<double>?> _beforeSendMetricDouble;

    internal SentryTraceMetricsCallbacks()
    {
        _beforeSendMetricByte = static traceMetric => traceMetric;
        _beforeSendMetricInt16 = static traceMetric => traceMetric;
        _beforeSendMetricInt32 = static traceMetric => traceMetric;
        _beforeSendMetricInt64 = static traceMetric => traceMetric;
        _beforeSendMetricSingle = static traceMetric => traceMetric;
        _beforeSendMetricDouble = static traceMetric => traceMetric;
    }

    internal void Set<T>(Func<SentryMetric<T>, SentryMetric<T>?> beforeSendMetric, IDiagnosticLogger? diagnosticLogger) where T : struct
    {
        beforeSendMetric ??= static traceMetric => traceMetric;

        if (typeof(T) == typeof(long))
        {
            _beforeSendMetricInt64 = (Func<SentryMetric<long>, SentryMetric<long>?>)(object)beforeSendMetric;
        }
        else if (typeof(T) == typeof(double))
        {
            _beforeSendMetricDouble = (Func<SentryMetric<double>, SentryMetric<double>?>)(object)beforeSendMetric;
        }
        else if (typeof(T) == typeof(int))
        {
            _beforeSendMetricInt32 = (Func<SentryMetric<int>, SentryMetric<int>?>)(object)beforeSendMetric;
        }
        else if (typeof(T) == typeof(float))
        {
            _beforeSendMetricSingle = (Func<SentryMetric<float>, SentryMetric<float>?>)(object)beforeSendMetric;
        }
        else if (typeof(T) == typeof(short))
        {
            _beforeSendMetricInt16 = (Func<SentryMetric<short>, SentryMetric<short>?>)(object)beforeSendMetric;
        }
        else if (typeof(T) == typeof(byte))
        {
            _beforeSendMetricByte = (Func<SentryMetric<byte>, SentryMetric<byte>?>)(object)beforeSendMetric;
        }
        else
        {
            diagnosticLogger?.LogWarning("{0} is unsupported type for Sentry Metrics. The only supported types are byte, short, int, long, float, and double.", typeof(T));
        }
    }

    internal SentryMetric<T>? Invoke<T>(SentryMetric<T> metric) where T : struct
    {
        if (typeof(T) == typeof(long))
        {
            return (SentryMetric<T>?)(object?)_beforeSendMetricInt64.Invoke((SentryMetric<long>)(object)metric);
        }

        if (typeof(T) == typeof(double))
        {
            return (SentryMetric<T>?)(object?)_beforeSendMetricDouble.Invoke((SentryMetric<double>)(object)metric);
        }

        if (typeof(T) == typeof(int))
        {
            return (SentryMetric<T>?)(object?)_beforeSendMetricInt32.Invoke((SentryMetric<int>)(object)metric);
        }

        if (typeof(T) == typeof(float))
        {
            return (SentryMetric<T>?)(object?)_beforeSendMetricSingle.Invoke((SentryMetric<float>)(object)metric);
        }

        if (typeof(T) == typeof(short))
        {
            return (SentryMetric<T>?)(object?)_beforeSendMetricInt16.Invoke((SentryMetric<short>)(object)metric);
        }

        if (typeof(T) == typeof(byte))
        {
            return (SentryMetric<T>?)(object?)_beforeSendMetricByte.Invoke((SentryMetric<byte>)(object)metric);
        }

        Debug.Fail($"Unhandled Metric Type {typeof(T)}.", "This instruction should be unreachable.");
        return null;
    }
}
