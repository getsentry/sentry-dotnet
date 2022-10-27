using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sentry.Internal.JsonConverters;

internal class UIntPtrJsonConverter : JsonConverter<UIntPtr>
{
    public override UIntPtr Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return new UIntPtr(reader.GetUInt64());
    }

    public override void Write(Utf8JsonWriter writer, UIntPtr value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value.ToUInt64());
    }
}
