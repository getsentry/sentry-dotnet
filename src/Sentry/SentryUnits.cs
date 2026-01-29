namespace Sentry;

/// <summary>
/// Supported units by Relay and Sentry.
/// Applies to <see cref="SentryMetric.Unit"/>, as well as attributes of <see cref="SentryLog"/> and attributes of <see cref="SentryMetric"/>.
/// </summary>
/// <remarks>
/// Contains the units currently <see href="https://getsentry.github.io/relay/relay_metrics/enum.MetricUnit.html">supported by Relay</see> and Sentry.
/// For <see href="https://develop.sentry.dev/sdk/telemetry/metrics/">Metrics</see> and <see href="https://develop.sentry.dev/sdk/telemetry/attributes/">Attributes</see>, currently, custom units are not allowed and will be set to "None" by Relay.
/// </remarks>
/// <seealso href="https://develop.sentry.dev/sdk/telemetry/attributes/#units"/>
/// <seealso href="https://github.com/getsentry/sentry-conventions/"/>
public static class SentryUnits
{
    /// <summary>
    /// Duration Units.
    /// </summary>
    public static class Duration
    {
        /// <summary>
        /// Nanosecond unit (ns).
        /// 10^-9 seconds.
        /// </summary>
        public static string Nanosecond { get; } = "nanosecond";

        /// <summary>
        /// Microsecond unit (μs).
        /// 10^-6 seconds.
        /// </summary>
        public static string Microsecond { get; } = "microsecond";

        /// <summary>
        /// Millisecond unit (ms).
        /// 10^-3 seconds.
        /// </summary>
        public static string Millisecond { get; } = "millisecond";

        /// <summary>
        /// Second unit (s).
        /// </summary>
        public static string Second { get; } = "second";

        /// <summary>
        /// Minute unit (min).
        /// 60 seconds.
        /// </summary>
        public static string Minute { get; } = "minute";

        /// <summary>
        /// Hour unit (h).
        /// 3_600 seconds.
        /// </summary>
        public static string Hour { get; } = "hour";

        /// <summary>
        /// Day unit (d).
        /// 86_400 seconds.
        /// </summary>
        public static string Day { get; } = "day";

        /// <summary>
        /// Week unit (wk).
        /// 604_800 seconds.
        /// </summary>
        public static string Week { get; } = "week";
    }

    /// <summary>
    /// Information Units.
    /// </summary>
    /// <remarks>
    /// Note that there are computer systems with a different number of bits per byte.
    /// </remarks>
    public static class Information
    {
        /// <summary>
        /// Bit unit (b).
        /// 1/8 of a byte.
        /// </summary>
        public static string Bit { get; } = "bit";

        /// <summary>
        /// Byte unit (B).
        /// 8 bits.
        /// </summary>
        public static string Byte { get; } = "byte";

        /// <summary>
        /// Kilobyte unit (kB).
        /// 10^3 bytes = 1_000 bytes (decimal).
        /// </summary>
        public static string Kilobyte { get; } = "kilobyte";

        /// <summary>
        /// Kibibyte unit (KiB).
        /// 2^10 bytes = 1_024 bytes (binary).
        /// </summary>
        public static string Kibibyte { get; } = "kibibyte";

        /// <summary>
        /// Megabyte unit (MB).
        /// 10^6 bytes = 1_000_000 bytes (decimal).
        /// </summary>
        public static string Megabyte { get; } = "megabyte";

        /// <summary>
        /// Mebibyte unit (MiB).
        /// 2^20 bytes = 1_048_576 bytes (binary).
        /// </summary>
        public static string Mebibyte { get; } = "mebibyte";

        /// <summary>
        /// Gigabyte unit (GB).
        /// 10^9 bytes = 1_000_000_000 bytes (decimal).
        /// </summary>
        public static string Gigabyte { get; } = "gigabyte";

        /// <summary>
        /// Gibibyte unit (GiB).
        /// 2^30 bytes = 1_073_741_824 bytes (binary).
        /// </summary>
        public static string Gibibyte { get; } = "gibibyte";

        /// <summary>
        /// Terabyte unit (TB).
        /// 10^12 bytes = 1_000_000_000_000 bytes (decimal).
        /// </summary>
        public static string Terabyte { get; } = "terabyte";

        /// <summary>
        /// Tebibyte unit (TiB).
        /// 2^40 bytes = 1_099_511_627_776 bytes (binary).
        /// </summary>
        public static string Tebibyte { get; } = "tebibyte";

        /// <summary>
        /// Petabyte unit (PB).
        /// 10^15 bytes = 1_000_000_000_000_000 bytes (decimal).
        /// </summary>
        public static string Petabyte { get; } = "petabyte";

        /// <summary>
        /// Pebibyte unit (PiB).
        /// 2^50 bytes = 1_125_899_906_842_624 bytes (binary).
        /// </summary>
        public static string Pebibyte { get; } = "pebibyte";

        /// <summary>
        /// Exabyte unit (EB).
        /// 10^18 bytes = 1_000_000_000_000_000_000 bytes (decimal).
        /// </summary>
        public static string Exabyte { get; } = "exabyte";

        /// <summary>
        /// Exbibyte unit (EiB).
        /// 2^60 bytes = 1_152_921_504_606_846_976 bytes (binary).
        /// </summary>
        public static string Exbibyte { get; } = "exbibyte";
    }

    /// <summary>
    /// Fraction Units.
    /// </summary>
    public static class Fraction
    {
        /// <summary>
        /// Ratio unit.
        /// Floating point fraction of 1.
        /// </summary>
        public static string Ratio { get; } = "ratio";

        /// <summary>
        /// Percent unit (%).
        /// Ratio expressed as a fraction of 100. 100% equals a ratio of 1.0.
        /// </summary>
        public static string Percent { get; } = "percent";
    }
}
