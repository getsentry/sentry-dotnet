using Sentry.Protocol;

namespace Sentry;

/// <summary>
/// Immutable data belonging to a transaction.
/// </summary>
public interface ITransactionData : ISpanData, ITransactionContext, IEventLike
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
/// Extensions for <see cref="ITransactionData"/>
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class TransactionDataExtensions
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
        transaction.SetMeasurement(name, new Measurement(value, unit));

    /// <inheritdoc cref="SetMeasurement(Sentry.ITransactionData,string,int,Sentry.MeasurementUnit)" />
    public static void SetMeasurement(this ITransactionData transaction, string name, long value,
        MeasurementUnit unit = default) =>
        transaction.SetMeasurement(name, new Measurement(value, unit));

    /// <inheritdoc cref="SetMeasurement(Sentry.ITransactionData,string,int,Sentry.MeasurementUnit)" />
#if !__MOBILE__
    // ulong parameter is not CLS compliant
    [CLSCompliant(false)]
#endif
    public static void SetMeasurement(this ITransactionData transaction, string name, ulong value,
        MeasurementUnit unit = default) =>
        transaction.SetMeasurement(name, new Measurement(value, unit));

    /// <inheritdoc cref="SetMeasurement(Sentry.ITransactionData,string,int,Sentry.MeasurementUnit)" />
    public static void SetMeasurement(this ITransactionData transaction, string name, double value,
        MeasurementUnit unit = default) =>
        transaction.SetMeasurement(name, new Measurement(value, unit));
}
