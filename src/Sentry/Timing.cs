using Sentry.Extensibility;
using Sentry.Internal;
using Sentry.Protocol;
using Sentry.Protocol.Metrics;

namespace Sentry;

/// <summary>
/// Measures the time it takes to run a given code block and emits this as a metric.
/// </summary>
/// <example>
/// using (var timing = new Timing("my-operation"))
/// {
///     ...
/// }
/// </example>
internal class Timing : IDisposable
{
    internal const string OperationName = "metric.timing";
    public const string MetricsOrigin = "auto.metrics";

    private readonly SentryOptions _options;
    private readonly MetricAggregator _metricAggregator;
    private readonly string _key;
    private readonly MeasurementUnit.Duration _unit;
    private readonly IDictionary<string, string>? _tags;
    internal readonly Stopwatch _stopwatch = new();
    private readonly ISpan _span;
    internal readonly DateTime _startTime = DateTime.UtcNow;

    /// <summary>
    /// Creates a new <see cref="Timing"/> instance.
    /// </summary>
    internal Timing(MetricAggregator metricAggregator, IMetricHub metricHub, SentryOptions options,
        string key, MeasurementUnit.Duration unit, IDictionary<string, string>? tags, int stackLevel)
    {
        _options = options;
        _metricAggregator = metricAggregator;
        _key = key;
        _unit = unit;
        _tags = tags;
        _stopwatch.Start();

        _span = metricHub.StartSpan(OperationName, key);
        _span.SetOrigin(MetricsOrigin);
        if (tags is not null)
        {
            _span.SetTags(tags);
        }

        // Report code locations here for better accuracy
        _metricAggregator.RecordCodeLocation(MetricType.Distribution, key, unit, stackLevel + 1, _startTime);
    }

    /// <inheritdoc cref="IDisposable"/>
    public void Dispose()
    {
        _stopwatch.Stop();
        DisposeInternal(_stopwatch.Elapsed);
    }

    internal void DisposeInternal(TimeSpan elapsed)
    {
        try
        {
            var value = _unit switch
            {
                MeasurementUnit.Duration.Week => elapsed.TotalDays / 7,
                MeasurementUnit.Duration.Day => elapsed.TotalDays,
                MeasurementUnit.Duration.Hour => elapsed.TotalHours,
                MeasurementUnit.Duration.Minute => elapsed.TotalMinutes,
                MeasurementUnit.Duration.Second => elapsed.TotalSeconds,
                MeasurementUnit.Duration.Millisecond => elapsed.TotalMilliseconds,
                MeasurementUnit.Duration.Microsecond => elapsed.TotalMilliseconds * 1000,
                MeasurementUnit.Duration.Nanosecond => elapsed.TotalMilliseconds * 1000000,
                _ => throw new ArgumentOutOfRangeException(nameof(_unit), _unit, null)
            };
            _metricAggregator.Timing(_key, value, _unit, _tags, _startTime);
        }
        catch (Exception e)
        {
            _options.LogError(e, "Error capturing timing '{0}'", _key);
        }
        finally
        {
            _span?.Finish();
        }
    }
}
