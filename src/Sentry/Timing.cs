using Sentry.Extensibility;
using Sentry.Protocol.Metrics;

namespace Sentry;

/// <summary>
/// Measures the time it takes to run a given code block and emits this as a metric. The <see cref="Timing"/> class is
/// designed to be used in a <c>using</c> statement.
/// </summary>
/// <example>
/// using (var timing = new Timing("my-operation"))
/// {
///     ...
/// }
/// </example>
public class Timing: IDisposable
{
    private readonly IHub _hub;
    private readonly string _key;
    private readonly MeasurementUnit.Duration _unit;
    private readonly IDictionary<string, string>? _tags;
    private readonly Stopwatch _stopwatch = new();
    private readonly ISpan _span;
    private readonly DateTime _startTime = DateTime.UtcNow;

    /// <summary>
    /// Creates a new <see cref="Timing"/> instance.
    /// </summary>
    public Timing(string key, MeasurementUnit.Duration unit = MeasurementUnit.Duration.Second,
        IDictionary<string, string>? tags = null)
        : this(SentrySdk.CurrentHub, key, unit, tags, stackLevel: 2 /* one for each constructor */)
    {
    }

    /// <summary>
    /// Creates a new <see cref="Timing"/> instance.
    /// </summary>
    public Timing(IHub hub, string key, MeasurementUnit.Duration unit = MeasurementUnit.Duration.Second,
        IDictionary<string, string>? tags = null)
    : this(hub, key, unit, tags, stackLevel: 2 /* one for each constructor */)
    {
    }

    internal Timing(IHub hub, string key, MeasurementUnit.Duration unit, IDictionary<string, string>? tags,
        int stackLevel)
    {
        _hub = hub;
        _key = key;
        _unit = unit;
        _tags = tags;
        _stopwatch.Start();

        ITransactionTracer? currentTransaction = null;
        hub.ConfigureScope(s => currentTransaction = s.Transaction);
        _span = currentTransaction is {} transaction
            ? transaction.StartChild("metric.timing", key)
            : hub.StartTransaction("metric.timing", key);
        if (tags is not null)
        {
            _span.SetTags(tags);
        }

        // Report code locations here for better accuracy
        if (hub.Metrics is MetricAggregator metrics)
        {
            metrics.RecordCodeLocation(MetricType.Distribution, key, unit, stackLevel + 1, _startTime);
        }
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
            _hub.Metrics.Timing(_key, value, _unit, _tags, _startTime);
        }
        catch (Exception e)
        {
            _hub.GetSentryOptions()?.LogError(e, "Error capturing timing '{0}'", _key);
        }
        finally
        {
            _span?.Finish();
        }
    }
}
