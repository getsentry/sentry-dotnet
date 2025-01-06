using Sentry.Infrastructure;

namespace Sentry;

#if MEMORY_DUMP_SUPPORTED

/// <summary>
/// A debouncer that can be used to limit the number of occurrences of an event within a given interval and optionally,
/// enforce a minimum cooldown period between events.
/// </summary>
[Experimental(DiagnosticId.ExperimentalFeature)]
public class Debouncer
{
    internal enum DebouncerInterval { Minute, Hour, Day, ApplicationLifetime }

    internal DateTimeOffset _intervalStart = DateTimeOffset.MinValue;
    internal DateTimeOffset _lastEvent = DateTimeOffset.MinValue;
    internal int _occurrences;

    internal readonly DebouncerInterval _intervalType;
    internal readonly int _eventMaximum;
    private readonly TimeSpan? _cooldown;

    private Debouncer(DebouncerInterval intervalType, int eventMaximum = 1, TimeSpan? cooldown = null)
    {
        _intervalType = intervalType;
        _cooldown = cooldown;
        _eventMaximum = eventMaximum;
    }

    /// <summary>
    /// Creates a debouncer that limits the number of events per minute
    /// </summary>
    /// <param name="eventMaximum">The maximum number of events that will be processed per minute</param>
    /// <param name="cooldown">An optional obligatory cooldown since the last event before any other events will be processed</param>
    /// <returns></returns>
    public static Debouncer PerMinute(int eventMaximum = 1, TimeSpan? cooldown = null)
        => new(DebouncerInterval.Minute, eventMaximum, cooldown);

    /// <summary>
    /// Creates a debouncer that limits the number of events per hour
    /// </summary>
    /// <param name="eventMaximum">The maximum number of events that will be processed per hour</param>
    /// <param name="cooldown">An optional obligatory cooldown since the last event before any other events will be processed</param>
    /// <returns></returns>
    public static Debouncer PerHour(int eventMaximum = 1, TimeSpan? cooldown = null)
        => new(DebouncerInterval.Hour, eventMaximum, cooldown);

    /// <summary>
    /// Creates a debouncer that limits the number of events per day
    /// </summary>
    /// <param name="eventMaximum">The maximum number of events that will be processed per day</param>
    /// <param name="cooldown">An optional obligatory cooldown since the last event before any other events will be processed</param>
    /// <returns></returns>
    public static Debouncer PerDay(int eventMaximum = 1, TimeSpan? cooldown = null)
        => new(DebouncerInterval.Day, eventMaximum, cooldown);

    /// <summary>
    /// Creates a debouncer that limits the number of events that will be processed for the lifetime of the application
    /// </summary>
    /// <param name="eventMaximum">The maximum number of events that will be processed</param>
    /// <param name="cooldown">An optional obligatory cooldown since the last event before any other events will be processed</param>
    /// <returns></returns>
    public static Debouncer PerApplicationLifetime(int eventMaximum = 1, TimeSpan? cooldown = null)
        => new(DebouncerInterval.ApplicationLifetime, eventMaximum, cooldown);

    private TimeSpan IntervalTimeSpan()
    {
        switch (_intervalType)
        {
            case DebouncerInterval.Minute:
                return TimeSpan.FromMinutes(1);
            case DebouncerInterval.Hour:
                return TimeSpan.FromHours(1);
            case DebouncerInterval.Day:
                return TimeSpan.FromDays(1);
            case DebouncerInterval.ApplicationLifetime:
                return TimeSpan.MaxValue;
            default:
                throw new ArgumentOutOfRangeException(nameof(_intervalType));
        }
    }

    internal void RecordOccurence(DateTimeOffset? timestamp = null)
    {
        var eventTime = timestamp ?? DateTimeOffset.UtcNow;

        if (eventTime - _intervalStart >= IntervalTimeSpan())
        {
            _intervalStart = eventTime;
            _occurrences = 0;
        }

        _occurrences++;
        _lastEvent = eventTime;
    }

    internal bool CanProcess(DateTimeOffset? timestamp = null)
    {
        if (_occurrences >= _eventMaximum)
        {
            return false;
        }

        var eventTime = timestamp ?? DateTimeOffset.UtcNow;
        return _cooldown is not { } cooldown || _lastEvent + cooldown <= eventTime;
    }
}

#endif
