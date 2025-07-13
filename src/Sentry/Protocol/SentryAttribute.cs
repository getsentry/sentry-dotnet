using Sentry.Extensibility;

namespace Sentry.Protocol;

[DebuggerDisplay(@"\{ Value = {Value}, Type = {Type} \}")]
internal readonly struct SentryAttribute
{
    internal static SentryAttribute CreateString(object value) => new(value, "string");
    internal static SentryAttribute CreateBoolean(object value) => new(value, "boolean");
    internal static SentryAttribute CreateInteger(object value) => new(value, "integer");
    internal static SentryAttribute CreateDouble(object value) => new(value, "double");

    public SentryAttribute(object value)
    {
        Value = value;
        Type = null;
    }

    public SentryAttribute(object value, string type)
    {
        Value = value;
        Type = type;
    }

    public object? Value { get; }
    public string? Type { get; }
}

internal static class SentryAttributeSerializer
{
    internal static void WriteStringAttribute(Utf8JsonWriter writer, string propertyName, string value)
    {
        writer.WritePropertyName(propertyName);
        writer.WriteStartObject();
        writer.WriteString("value", value);
        writer.WriteString("type", "string");
        writer.WriteEndObject();
    }

    internal static void WriteAttribute(Utf8JsonWriter writer, string propertyName, SentryAttribute attribute, IDiagnosticLogger? logger)
    {
        if (attribute.Value is null)
        {
            logger?.LogWarning("'null' is not supported by Sentry-Attributes and will be ignored.");
            return;
        }

        writer.WritePropertyName(propertyName);
        WriteAttributeValue(writer, attribute.Value, attribute.Type, logger);
    }

    internal static void WriteAttribute(Utf8JsonWriter writer, string propertyName, object? value, IDiagnosticLogger? logger)
    {
        if (value is null)
        {
            logger?.LogWarning("'null' is not supported by Sentry-Attributes and will be ignored.");
            return;
        }

        writer.WritePropertyName(propertyName);
        WriteAttributeValue(writer, value, logger);
    }

    private static void WriteAttributeValue(Utf8JsonWriter writer, object value, string? type, IDiagnosticLogger? logger)
    {
        if (type == "string")
        {
            writer.WriteStartObject();
            writer.WriteString("value", (string)value);
            writer.WriteString("type", type);
            writer.WriteEndObject();
        }
        else
        {
            WriteAttributeValue(writer, value, logger);
        }
    }

    private static void WriteAttributeValue(Utf8JsonWriter writer, object value, IDiagnosticLogger? logger)
    {
        writer.WriteStartObject();

        // covering most built-in types of .NET with C# language support
        // for `net7.0` or greater, we could utilize "Generic Math" in the future, if there is demand
        // see documentation for supported types: https://develop.sentry.dev/sdk/telemetry/logs/
        if (value is string @string)
        {
            writer.WriteString("value", @string);
            writer.WriteString("type", "string");
        }
        else if (value is char @char)
        {
#if NET7_0_OR_GREATER
            writer.WriteString("value", new ReadOnlySpan<char>(in @char));
#elif (NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER)
            writer.WriteString("value", MemoryMarshal.CreateReadOnlySpan(ref @char, 1));
#else
            writer.WriteString("value", @char.ToString(CultureInfo.InvariantCulture));
#endif
            writer.WriteString("type", "string");
        }
        else if (value is bool boolean)
        {
            writer.WriteBoolean("value", boolean);
            writer.WriteString("type", "boolean");
        }
        else if (value is sbyte @sbyte)
        {
            writer.WriteNumber("value", @sbyte);
            writer.WriteString("type", "integer");
        }
        else if (value is byte @byte)
        {
            writer.WriteNumber("value", @byte);
            writer.WriteString("type", "integer");
        }
        else if (value is short int16)
        {
            writer.WriteNumber("value", int16);
            writer.WriteString("type", "integer");
        }
        else if (value is ushort uint16)
        {
            writer.WriteNumber("value", uint16);
            writer.WriteString("type", "integer");
        }
        else if (value is int int32)
        {
            writer.WriteNumber("value", int32);
            writer.WriteString("type", "integer");
        }
        else if (value is uint uint32)
        {
            writer.WriteNumber("value", uint32);
            writer.WriteString("type", "integer");
        }
        else if (value is long int64)
        {
            writer.WriteNumber("value", int64);
            writer.WriteString("type", "integer");
        }
        else if (value is ulong uint64)
        {
            writer.WriteString("value", uint64.ToString(NumberFormatInfo.InvariantInfo));
            writer.WriteString("type", "string");

            logger?.LogWarning("Type 'ulong' (unsigned 64-bit integer) is not supported by Sentry-Attributes due to possible overflows. Using 'ToString' and type=string. Please use a supported numeric type instead. To suppress this message, convert the value of this Attribute to type string explicitly.");
        }
        else if (value is nint intPtr)
        {
            writer.WriteNumber("value", intPtr);
            writer.WriteString("type", "integer");
        }
        else if (value is nuint uintPtr)
        {
#if NET5_0_OR_GREATER
            writer.WriteString("value", uintPtr.ToString(NumberFormatInfo.InvariantInfo));
#else
            writer.WriteString("value", uintPtr.ToString());
#endif
            writer.WriteString("type", "string");

            logger?.LogWarning("Type 'nuint' (unsigned platform-dependent integer) is not supported by Sentry-Attributes due to possible overflows on 64-bit processes. Using 'ToString' and type=string. Please use a supported numeric type instead. To suppress this message, convert the value of this Attribute to type string explicitly.");
        }
        else if (value is float single)
        {
            writer.WriteNumber("value", single);
            writer.WriteString("type", "double");
        }
        else if (value is double @double)
        {
            writer.WriteNumber("value", @double);
            writer.WriteString("type", "double");
        }
        else if (value is decimal @decimal)
        {
            writer.WriteString("value", @decimal.ToString(NumberFormatInfo.InvariantInfo));
            writer.WriteString("type", "string");

            logger?.LogWarning("Type 'decimal' (128-bit floating-point) is not supported by Sentry-Attributes due to possible overflows. Using 'ToString' and type=string. Please use a supported numeric type instead. To suppress this message, convert the value of this Attribute to type string explicitly.");
        }
        else
        {
            writer.WriteString("value", value.ToString());
            writer.WriteString("type", "string");

            logger?.LogWarning("Type '{0}' is not supported by Sentry-Attributes. Using 'ToString' and type=string. Please use a supported type instead. To suppress this message, convert the value of this Attribute to type string explicitly.", value.GetType());
        }

        writer.WriteEndObject();
    }
}
