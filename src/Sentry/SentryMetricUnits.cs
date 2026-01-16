namespace Sentry;

/// <summary>
/// Provides constants for metric units supported by Sentry.
/// </summary>
/// <remarks>
/// While the SDK accepts any string value for metric units, these constants provide
/// type-safe access to units that are officially supported by Sentry's Relay and platform.
/// </remarks>
/// <seealso href="https://develop.sentry.dev/sdk/telemetry/metrics/"/>
/// <seealso href="https://develop.sentry.dev/sdk/telemetry/attributes/#units"/>
public static class SentryMetricUnits
{
    /// <summary>
    /// Duration units for time measurements.
    /// </summary>
    public static class Duration
    {
        /// <summary>
        /// Nanosecond unit (ns).
        /// </summary>
        public static string Nanosecond { get; } = "nanosecond";

        /// <summary>
        /// Microsecond unit (μs).
        /// </summary>
        public static string Microsecond { get; } = "microsecond";

        /// <summary>
        /// Millisecond unit (ms).
        /// </summary>
        public static string Millisecond { get; } = "millisecond";

        /// <summary>
        /// Second unit (s).
        /// </summary>
        public static string Second { get; } = "second";

        /// <summary>
        /// Minute unit (min).
        /// </summary>
        public static string Minute { get; } = "minute";

        /// <summary>
        /// Hour unit (h).
        /// </summary>
        public static string Hour { get; } = "hour";

        /// <summary>
        /// Day unit (d).
        /// </summary>
        public static string Day { get; } = "day";

        /// <summary>
        /// Week unit (wk).
        /// </summary>
        public static string Week { get; } = "week";
    }

    /// <summary>
    /// Information units for data storage and transfer measurements.
    /// </summary>
    public static class Information
    {
        /// <summary>
        /// Bit unit (b).
        /// </summary>
        public static string Bit { get; } = "bit";

        /// <summary>
        /// Byte unit (B).
        /// </summary>
        public static string Byte { get; } = "byte";

        /// <summary>
        /// Kilobyte unit (KB) - 1,000 bytes (decimal).
        /// </summary>
        public static string Kilobyte { get; } = "kilobyte";

        /// <summary>
        /// Kibibyte unit (KiB) - 1,024 bytes (binary).
        /// </summary>
        public static string Kibibyte { get; } = "kibibyte";

        /// <summary>
        /// Megabyte unit (MB) - 1,000,000 bytes (decimal).
        /// </summary>
        public static string Megabyte { get; } = "megabyte";

        /// <summary>
        /// Mebibyte unit (MiB) - 1,048,576 bytes (binary).
        /// </summary>
        public static string Mebibyte { get; } = "mebibyte";

        /// <summary>
        /// Gigabyte unit (GB) - 1,000,000,000 bytes (decimal).
        /// </summary>
        public static string Gigabyte { get; } = "gigabyte";

        /// <summary>
        /// Gibibyte unit (GiB) - 1,073,741,824 bytes (binary).
        /// </summary>
        public static string Gibibyte { get; } = "gibibyte";

        /// <summary>
        /// Terabyte unit (TB) - 1,000,000,000,000 bytes (decimal).
        /// </summary>
        public static string Terabyte { get; } = "terabyte";

        /// <summary>
        /// Tebibyte unit (TiB) - 1,099,511,627,776 bytes (binary).
        /// </summary>
        public static string Tebibyte { get; } = "tebibyte";

        /// <summary>
        /// Petabyte unit (PB) - 1,000,000,000,000,000 bytes (decimal).
        /// </summary>
        public static string Petabyte { get; } = "petabyte";

        /// <summary>
        /// Pebibyte unit (PiB) - 1,125,899,906,842,624 bytes (binary).
        /// </summary>
        public static string Pebibyte { get; } = "pebibyte";

        /// <summary>
        /// Exabyte unit (EB) - 1,000,000,000,000,000,000 bytes (decimal).
        /// </summary>
        public static string Exabyte { get; } = "exabyte";

        /// <summary>
        /// Exbibyte unit (EiB) - 1,152,921,504,606,846,976 bytes (binary).
        /// </summary>
        public static string Exbibyte { get; } = "exbibyte";
    }

    /// <summary>
    /// Fraction units for ratio and percentage measurements.
    /// </summary>
    public static class Fraction
    {
        /// <summary>
        /// Ratio unit (unitless fraction).
        /// </summary>
        public static string Ratio { get; } = "ratio";

        /// <summary>
        /// Percent unit (%).
        /// </summary>
        public static string Percent { get; } = "percent";
    }
}
