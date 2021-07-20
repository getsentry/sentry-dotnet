using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sentry.Internal
{
    /// <summary>
    /// Replace the data value of the T object by null.
    /// </summary>
    /// <typeparam name="T">The class to be scrubbed</typeparam>
    internal class JsonConverterScrubber<T> : JsonConverter<T?>
    {
        public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => default;

        public override void Write(Utf8JsonWriter writer, T? value, JsonSerializerOptions options)
        {
            writer.WriteNullValue();
        }
    }

    /// <summary>
    /// Filter for removing the serialized value of the T object if their namespace comes from System.Reflection.<br/>
    /// The value will be replaced by null.
    /// </summary>
    /// <typeparam name="T">The class to be filtered</typeparam>
    internal class JsonConverterFilterReflection<T> : JsonConverter<T?>
    {
        public override bool CanConvert(Type typeToConvert)
            => typeToConvert?.FullName?.StartsWith("System.Reflection") == true;

        public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => default;

        public override void Write(Utf8JsonWriter writer, T? value, JsonSerializerOptions options)
        {
            writer.WriteNullValue();
        }
    }
}
