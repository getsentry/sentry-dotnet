using Sentry.Protocol;

namespace Sentry;

/// <summary>
/// Immutable data belonging to a span.
/// </summary>
public interface ISpanData : ITraceContext, IHasTags, IHasExtra
{
    /// <summary>
    /// Start timestamp.
    /// </summary>
    DateTimeOffset StartTimestamp { get; }

    /// <summary>
    /// End timestamp.
    /// </summary>
    DateTimeOffset? EndTimestamp { get; }

    /// <summary>
    /// Whether the span is finished.
    /// </summary>
    bool IsFinished { get; }

    /// <summary>
    /// Get Sentry trace header.
    /// </summary>
    SentryTraceHeader GetTraceHeader();

    /// <summary>
    /// The measurements that have been set on the transaction.
    /// </summary>
    IReadOnlyDictionary<string, Measurement> Measurements { get; }

    /// <summary>
    /// Sets a measurement on the transaction.
    /// </summary>
    /// <param name="name">The name of the measurement.</param>
    /// <param name="measurement">The measurement.</param>
    void SetMeasurement(string name, Measurement measurement);
}

/// <summary>
/// Extensions for <see cref="ISpanData"/>
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class SpanDataExtensions
{
    /// <summary>
    /// Sets a measurement on the transaction.
    /// </summary>
    /// <param name="transaction">The transaction.</param>
    /// <param name="name">The name of the measurement.</param>
    /// <param name="value">The value of the measurement.</param>
    /// <param name="unit">The optional unit of the measurement.</param>
    public static void SetMeasurement(this ISpanData spanData, string name, int value,
        MeasurementUnit unit = default) =>
        spanData.SetMeasurement(name, new Measurement(value, unit));

    /// <inheritdoc cref="SetMeasurement(Sentry.ISpanData,string,int,Sentry.MeasurementUnit)" />
    public static void SetMeasurement(this ISpanData spanData, string name, long value,
        MeasurementUnit unit = default) =>
        spanData.SetMeasurement(name, new Measurement(value, unit));

    /// <inheritdoc cref="SetMeasurement(Sentry.ISpanData,string,int,Sentry.MeasurementUnit)" />
#if !__MOBILE__
    // ulong parameter is not CLS compliant
    [CLSCompliant(false)]
#endif
    public static void SetMeasurement(this ISpanData spanData, string name, ulong value,
        MeasurementUnit unit = default) =>
        spanData.SetMeasurement(name, new Measurement(value, unit));

    /// <inheritdoc cref="SetMeasurement(Sentry.ISpanData,string,int,Sentry.MeasurementUnit)" />
    public static void SetMeasurement(this ISpanData spanData, string name, double value,
        MeasurementUnit unit = default) =>
        spanData.SetMeasurement(name, new Measurement(value, unit));
}
