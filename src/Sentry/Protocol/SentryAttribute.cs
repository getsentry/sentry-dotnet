namespace Sentry.Protocol;

internal readonly struct SentryAttribute
{
    public SentryAttribute(object value, string type)
    {
        Value = value;
        Type = type;
    }

    public object Value { get; }
    public string Type { get; }
}

internal static class SentryAttributeSerializer
{
    internal static void WriteAttribute(Utf8JsonWriter writer, string propertyName, SentryAttribute attribute)
    {
        Debug.Assert(attribute.Type is not null);
        writer.WritePropertyName(propertyName);
        WriteAttributeValue(writer, attribute.Value, attribute.Type);
    }

    internal static void WriteAttribute(Utf8JsonWriter writer, string propertyName, object value, string type)
    {
        writer.WritePropertyName(propertyName);
        WriteAttributeValue(writer, value, type);
    }

    internal static void WriteAttribute(Utf8JsonWriter writer, string propertyName, object value)
    {
        writer.WritePropertyName(propertyName);
        WriteAttributeValue(writer, value);
    }

    private static void WriteAttributeValue(Utf8JsonWriter writer, object value, string type)
    {
        writer.WriteStartObject();

        if (type == "string")
        {
            writer.WriteString("value", (string)value);
            writer.WriteString("type", type);
        }
        else if (type == "boolean")
        {
            writer.WriteBoolean("value", (bool)value);
            writer.WriteString("type", type);
        }
        else if (type == "integer")
        {
            writer.WriteNumber("value", (long)value);
            writer.WriteString("type", type);
        }
        else if (type == "double")
        {
            writer.WriteNumber("value", (double)value);
            writer.WriteString("type", type);
        }
        else
        {
            writer.WriteString("value", value.ToString());
            writer.WriteString("type", "string");
        }

        writer.WriteEndObject();
    }

    private static void WriteAttributeValue(Utf8JsonWriter writer, object value)
    {
        writer.WriteStartObject();

        if (value is string str)
        {
            writer.WriteString("value", str);
            writer.WriteString("type", "string");
        }
        else if (value is bool boolean)
        {
            writer.WriteBoolean("value", boolean);
            writer.WriteString("type", "boolean");
        }
        else if (value is int int32)
        {
            writer.WriteNumber("value", int32);
            writer.WriteString("type", "integer");
        }
        else if (value is long int64)
        {
            writer.WriteNumber("value", int64);
            writer.WriteString("type", "integer");
        }
        else if (value is double float64)
        {
            writer.WriteNumber("value", float64);
            writer.WriteString("type", "double");
        }
        else
        {
            writer.WriteString("value", value.ToString());
            writer.WriteString("type", "string");
        }

        writer.WriteEndObject();
    }
}
