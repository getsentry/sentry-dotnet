namespace Sentry;

public readonly partial struct MeasurementUnit
{
    /// <summary>
    /// A fraction unit
    /// </summary>
    /// <seealso href="https://getsentry.github.io/relay/relay_metrics/enum.FractionUnit.html"/>
    public enum Fraction
    {
        /// <summary>
        /// Floating point fraction of 1.
        /// A ratio of 1.0 equals 100%.
        /// </summary>
        Ratio,

        /// <summary>
        /// Ratio expressed as a fraction of 100.
        /// 100% equals a ratio of 1.0.
        /// </summary>
        Percent
    }

    /// <summary>
    /// Implicitly casts a <see cref="MeasurementUnit.Fraction"/> to a <see cref="MeasurementUnit"/>.
    /// </summary>
    public static implicit operator MeasurementUnit(Fraction unit) => new(unit);
}
