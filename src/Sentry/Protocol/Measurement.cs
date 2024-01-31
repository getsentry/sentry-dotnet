using Sentry.Extensibility;
using Sentry.Internal.Extensions;

namespace Sentry.Protocol;

/// <summary>
/// A measurement, containing a numeric value and a unit.
/// </summary>
public sealed class Measurement : IJsonSerializable
{
    /// <summary>
    /// The numeric value of the measurement.
    /// </summary>
    public object Value { get; }

    /// <summary>
    /// The unit of measurement.
    /// </summary>
    public MeasurementUnit Unit { get; }

    private Measurement(object value, MeasurementUnit unit)
    {
        Value = value;
        Unit = unit;
    }

    internal Measurement(int value, MeasurementUnit unit = default)
    {
        Value = value;
        Unit = unit;
    }

    internal Measurement(long value, MeasurementUnit unit = default)
    {
        Value = value;
        Unit = unit;
    }

    internal Measurement(ulong value, MeasurementUnit unit = default)
    {
        Value = value;
        Unit = unit;
    }

    internal Measurement(double value, MeasurementUnit unit = default)
    {
        Value = value;
        Unit = unit;
    }

    /// <inheritdoc />
    public void WriteTo(Utf8JsonWriter writer, IDiagnosticLogger? logger)
    {
        writer.WriteStartObject();

        switch (Value)
        {
            case int number:
                writer.WriteNumber("value", number);
                break;
            case long number:
                writer.WriteNumber("value", number);
                break;
            case ulong number:
                writer.WriteNumber("value", number);
                break;
            case double number:
                writer.WriteNumber("value", number);
                break;
        }

        writer.WriteStringIfNotWhiteSpace("unit", Unit.ToString());

        writer.WriteEndObject();
    }

    /// <summary>
    /// Parses from JSON.
    /// </summary>
    public static Measurement FromJson(JsonElement json)
    {
        var value = json.GetProperty("value").GetDynamicOrNull()!;
        var unit = json.GetPropertyOrNull("unit")?.GetString();
        return new Measurement(value, MeasurementUnit.Parse(unit));
    }
}
