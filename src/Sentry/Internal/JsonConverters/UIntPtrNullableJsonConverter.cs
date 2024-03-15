namespace Sentry.Internal.JsonConverters;

internal class UIntPtrNullableJsonConverter : JsonConverter<UIntPtr?>
{
    public override UIntPtr? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        return new UIntPtr(reader.GetUInt64());
    }

    public override void Write(Utf8JsonWriter writer, UIntPtr? value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
        }
        else
        {
            writer.WriteNumberValue(value.Value.ToUInt64());
        }
    }
}
