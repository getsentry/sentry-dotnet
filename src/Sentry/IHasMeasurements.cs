using Sentry.Protocol;

namespace Sentry
{
    /// <summary>
    /// Interface for transactions that can keep track of measurements.
    /// </summary>
    /// <remarks>
    /// Ideally, this would just be implemented as part of <see cref="ITransactionData"/>.
    /// However, adding a property to a public interface is a breaking change.  We can do that in a future major version.
    /// </remarks>
    internal interface IHasMeasurements
    {
        /// <summary>
        /// The measurements that have been set on the transaction.
        /// </summary>
        IReadOnlyDictionary<string, Measurement> Measurements { get; }

        /// <summary>
        /// Sets a measurement on the transaction.
        /// </summary>
        /// <param name="name">The name of the measurement.</param>
        /// <param name="measurement">The measurement.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        void SetMeasurement(string name, Measurement measurement);
    }

    /// <summary>
    /// Extensions for <see cref="IHasMeasurements"/>
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class MeasurementExtensions
    {
        /// <summary>
        /// Sets a measurement on the transaction.
        /// </summary>
        /// <param name="transaction">The transaction.</param>
        /// <param name="name">The name of the measurement.</param>
        /// <param name="value">The value of the measurement.</param>
        /// <param name="unit">The optional unit of the measurement.</param>
        public static void SetMeasurement(this ITransactionData transaction, string name, int value,
            MeasurementUnit unit = default) =>
            (transaction as IHasMeasurements)?.SetMeasurement(name, new Measurement(value, unit));

        /// <inheritdoc cref="SetMeasurement(Sentry.ITransactionData,string,int,Sentry.MeasurementUnit)" />
        public static void SetMeasurement(this ITransactionData transaction, string name, long value,
            MeasurementUnit unit = default) =>
            (transaction as IHasMeasurements)?.SetMeasurement(name, new Measurement(value, unit));

        /// <inheritdoc cref="SetMeasurement(Sentry.ITransactionData,string,int,Sentry.MeasurementUnit)" />
#if !__MOBILE__
        // ulong parameter is not CLS compliant
        [CLSCompliant(false)]
#endif
        public static void SetMeasurement(this ITransactionData transaction, string name, ulong value,
            MeasurementUnit unit = default) =>
            (transaction as IHasMeasurements)?.SetMeasurement(name, new Measurement(value, unit));

        /// <inheritdoc cref="SetMeasurement(Sentry.ITransactionData,string,int,Sentry.MeasurementUnit)" />
        public static void SetMeasurement(this ITransactionData transaction, string name, double value,
            MeasurementUnit unit = default) =>
            (transaction as IHasMeasurements)?.SetMeasurement(name, new Measurement(value, unit));
    }
}
