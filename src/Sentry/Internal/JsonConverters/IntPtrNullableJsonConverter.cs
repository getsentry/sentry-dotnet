namespace Sentry.Internal.JsonConverters;

internal class IntPtrNullableJsonConverter : JsonConverter<IntPtr?>
{
    public override IntPtr? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        return new IntPtr(reader.GetInt64());
    }

    public override void Write(Utf8JsonWriter writer, IntPtr? value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
        }
        else
        {
            writer.WriteNumberValue(value.Value.ToInt64());
        }
    }
}
