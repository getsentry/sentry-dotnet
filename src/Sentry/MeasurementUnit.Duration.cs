namespace Sentry;

public readonly partial struct MeasurementUnit
{
    /// <summary>
    /// A time duration unit
    /// </summary>
    /// <seealso href="https://getsentry.github.io/relay/relay_metrics/enum.DurationUnit.html"/>
    public enum Duration
    {
        /// <summary>
        /// Nanosecond unit (10^-9 seconds)
        /// </summary>
        Nanosecond,

        /// <summary>
        /// Microsecond unit (10^-6 seconds)
        /// </summary>
        Microsecond,

        /// <summary>
        /// Millisecond unit (10^-3 seconds)
        /// </summary>
        Millisecond,

        /// <summary>
        /// Second unit
        /// </summary>
        Second,

        /// <summary>
        /// Minute unit (60 seconds)
        /// </summary>
        Minute,

        /// <summary>
        /// Hour unit (3,600 seconds)
        /// </summary>
        Hour,

        /// <summary>
        /// Day unit (86,400 seconds)
        /// </summary>
        Day,

        /// <summary>
        /// Week unit (604,800 seconds)
        /// </summary>
        Week
    }

    /// <summary>
    /// Implicitly casts a <see cref="MeasurementUnit.Duration"/> to a <see cref="MeasurementUnit"/>.
    /// </summary>
    public static implicit operator MeasurementUnit(Duration unit) => new(unit);
}
