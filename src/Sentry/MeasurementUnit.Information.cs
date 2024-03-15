namespace Sentry;

public readonly partial struct MeasurementUnit
{
    /// <summary>
    /// An information size unit
    /// </summary>
    /// <seealso href="https://getsentry.github.io/relay/relay_metrics/enum.InformationUnit.html"/>
    public enum Information
    {
        /// <summary>
        /// Bit unit (1/8 of byte)
        /// </summary>
        /// <remarks>
        /// Some computer systems may have a different number of bits per byte.
        /// </remarks>
        Bit,

        /// <summary>
        /// Byte unit
        /// </summary>
        Byte,

        /// <summary>
        /// Kilobyte unit (10^3 bytes)
        /// </summary>
        Kilobyte,

        /// <summary>
        /// Kibibyte unit (2^10 bytes)
        /// </summary>
        Kibibyte,

        /// <summary>
        /// Megabyte unit (10^6 bytes)
        /// </summary>
        Megabyte,

        /// <summary>
        /// Mebibyte unit (2^20 bytes)
        /// </summary>
        Mebibyte,

        /// <summary>
        /// Gigabyte unit (10^9 bytes)
        /// </summary>
        Gigabyte,

        /// <summary>
        /// Gibibyte unit (2^30 bytes)
        /// </summary>
        Gibibyte,

        /// <summary>
        /// Terabyte unit (10^12 bytes)
        /// </summary>
        Terabyte,

        /// <summary>
        /// Tebibyte unit (2^40 bytes)
        /// </summary>
        Tebibyte,

        /// <summary>
        /// Petabyte unit (10^15 bytes)
        /// </summary>
        Petabyte,

        /// <summary>
        /// Pebibyte unit (2^50 bytes)
        /// </summary>
        Pebibyte,

        /// <summary>
        /// Exabyte unit (10^18 bytes)
        /// </summary>
        Exabyte,

        /// <summary>
        /// Exbibyte unit (2^60 bytes)
        /// </summary>
        Exbibyte
    }

    /// <summary>
    /// Implicitly casts a <see cref="MeasurementUnit.Information"/> to a <see cref="MeasurementUnit"/>.
    /// </summary>
    public static implicit operator MeasurementUnit(Information unit) => new(unit);
}
