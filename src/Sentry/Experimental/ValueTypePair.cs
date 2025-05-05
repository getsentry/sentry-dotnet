using Sentry.Extensibility;

namespace Sentry.Experimental;

//TODO: remove? perhaps a simple System.ValueTuple`2 suffices
internal readonly struct ValueTypePair : ISentryJsonSerializable
{
    public ValueTypePair(object value, string type)
    {
        Value = value.ToString()!;
        Type = type;
    }

    public string Value { get; }
    public string Type { get; }

    public void WriteTo(Utf8JsonWriter writer, IDiagnosticLogger? logger)
    {
        writer.WriteStartObject();

        writer.WriteString("value", Value);
        writer.WriteString("type", Type);

        writer.WriteEndObject();
    }
}
