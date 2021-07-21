using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sentry.Internal
{
    /// <summary>
    /// Replace the data value of the T object by null.
    /// </summary>
    internal class SentryJsonConverter : JsonConverter<object?>
    {
        private bool IsAssignedFrom<T>(Type typeToConvert)
            => typeof(T).IsAssignableFrom(typeToConvert);

        public override bool CanConvert(Type typeToConvert) =>
            IsAssignedFrom<Type>(typeToConvert) ||
//            IsAssignedFrom<Exception>(typeToConvert) ||
            typeToConvert.FullName?.StartsWith("System.Reflection") == true;

        public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => default;

        public override void Write(Utf8JsonWriter writer, object? value, JsonSerializerOptions options)
        {
            if (value is Type type)
            {
                writer.WriteStringValue(GetTypeString(type));
            }
            else
            {
                writer.WriteNullValue();
            }
        }

        public string GetTypeString(Type type)
            => type.FullName ?? type.DeclaringType?.FullName ?? type.Name;
    }
}
