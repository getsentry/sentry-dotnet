using Sentry.Extensibility;
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
    private readonly IMetricHub _metricHub;
    private readonly SentryOptions _options;
    private readonly MetricAggregator _metricAggregator;
    private readonly string _key;
    private readonly MeasurementUnit.Duration _unit;
    private readonly IDictionary<string, string>? _tags;
    private readonly Stopwatch _stopwatch = new();
    private readonly ISpan _span;
    private readonly DateTime _startTime = DateTime.UtcNow;

    /// <summary>
    /// Creates a new <see cref="Timing"/> instance.
    /// </summary>
    internal Timing(MetricAggregator metricAggregator, IMetricHub metricHub, SentryOptions options,
        string key, MeasurementUnit.Duration unit, IDictionary<string, string>? tags, int stackLevel)
    {
        _metricHub = metricHub;
        _options = options;
        _metricAggregator = metricAggregator;
        _key = key;
        _unit = unit;
        _tags = tags;
        _stopwatch.Start();


        _span = metricHub.StartSpan("metric.timing", key);
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

        try
        {
            var value = _unit switch
            {
                MeasurementUnit.Duration.Week => _stopwatch.Elapsed.TotalDays / 7,
                MeasurementUnit.Duration.Day => _stopwatch.Elapsed.TotalDays,
                MeasurementUnit.Duration.Hour => _stopwatch.Elapsed.TotalHours,
                MeasurementUnit.Duration.Minute => _stopwatch.Elapsed.TotalMinutes,
                MeasurementUnit.Duration.Second => _stopwatch.Elapsed.TotalSeconds,
                MeasurementUnit.Duration.Millisecond => _stopwatch.Elapsed.TotalMilliseconds,
                MeasurementUnit.Duration.Microsecond => _stopwatch.Elapsed.TotalMilliseconds * 1000,
                MeasurementUnit.Duration.Nanosecond => _stopwatch.Elapsed.TotalMilliseconds * 1000000,
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
