using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sentry.Internal
{
    /// <summary>
    /// Replace the data value of the T object by null.
    /// </summary>
    internal class JsonConverterScrubber : JsonConverter<object?>
    {
        private readonly Func<Type, bool> _canConvert;

        public JsonConverterScrubber(Func<Type, bool> canConvert) =>
            _canConvert = canConvert;

        public override bool CanConvert(Type typeToConvert) => _canConvert(typeToConvert);

        public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => default;

        public override void Write(Utf8JsonWriter writer, object? value, JsonSerializerOptions options)
            => writer.WriteNullValue();
    }
}
