using Sentry.Extensibility;

namespace Sentry;

/// <summary>
/// Internal generic representation of <see cref="SentryMetric"/>.
/// </summary>
/// <typeparam name="T">The numeric type of the metric.</typeparam>
/// <remarks>
/// We hide some of the generic implementation details from user code.
/// </remarks>
internal sealed class SentryMetric<T> : SentryMetric where T : struct
{
    private readonly T _value;

    [SetsRequiredMembers]
    internal SentryMetric(DateTimeOffset timestamp, SentryId traceId, SentryMetricType type, string name, T value)
        : base(timestamp, traceId, type, name)
    {
        _value = value;
    }

    internal override object Value => _value;

    /// <inheritdoc />
    public override bool TryGetValue<TValue>(out TValue value) where TValue : struct
    {
        if (_value is TValue match)
        {
            value = match;
            return true;
        }

        value = default;
        return false;
    }

    private protected override void WriteMetricValueTo(Utf8JsonWriter writer, IDiagnosticLogger? logger)
    {
        const string propertyName = "value";
        var type = typeof(T);

        if (type == typeof(long))
        {
            writer.WriteNumber(propertyName, (long)(object)_value);
        }
        else if (type == typeof(double))
        {
            writer.WriteNumber(propertyName, (double)(object)_value);
        }
        else if (type == typeof(int))
        {
            writer.WriteNumber(propertyName, (int)(object)_value);
        }
        else if (type == typeof(float))
        {
            writer.WriteNumber(propertyName, (float)(object)_value);
        }
        else if (type == typeof(short))
        {
            writer.WriteNumber(propertyName, (short)(object)_value);
        }
        else if (type == typeof(byte))
        {
            writer.WriteNumber(propertyName, (byte)(object)_value);
        }
        else
        {
            Debug.Fail($"Unhandled Metric Type {typeof(T)}.", "This instruction should be unreachable.");
        }
    }
}
