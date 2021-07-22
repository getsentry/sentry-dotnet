using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sentry.Internal
{
    /// <summary>
    /// A converter that removes dangerous classes from being serialized,
    /// and, also formats some classes like Exception and Type.
    /// </summary>
    internal class SentryJsonConverter : JsonConverter<object?>
    {
        public override bool CanConvert(Type typeToConvert) =>
            typeof(Type).IsAssignableFrom(typeToConvert) ||
            typeToConvert.FullName?.StartsWith("System.Reflection") == true;

        public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => default;

        public override void Write(Utf8JsonWriter writer, object? value, JsonSerializerOptions options)
        {
            if (value is Type type &&
                type.FullName != null)
            {
                writer.WriteStringValue(type.FullName);
            }
            else
            {
                writer.WriteNullValue();
            }
        }
    }
}
