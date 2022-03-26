using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using Sentry.Extensibility;

namespace Sentry.Internal.Extensions
{
    internal static class JsonExtensions
    {
        // The Json options with a preset of rules that will remove dangerous and problematic
        // data from the serialized object.
        private static JsonSerializerOptions serializerOption = new() { Converters = { new SentryJsonConverter() } };

        public static void Deconstruct(this JsonProperty jsonProperty, out string name, out JsonElement value)
        {
            name = jsonProperty.Name;
            value = jsonProperty.Value;
        }

        public static IReadOnlyDictionary<string, object?>? GetDictionaryOrNull(this JsonElement json)
        {
            if (json.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            var result = new Dictionary<string, object?>();

            foreach (var (name, value) in json.EnumerateObject())
            {
                result[name] = value.GetDynamicOrNull();
            }

            return result;
        }

        public static IReadOnlyDictionary<string, string?>? GetStringDictionaryOrNull(this JsonElement json)
        {
            if (json.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            var result = new Dictionary<string, string?>(StringComparer.Ordinal);

            foreach (var (name, value) in json.EnumerateObject())
            {
                result[name] = value.GetString();
            }

            return result;
        }

        public static JsonElement? GetPropertyOrNull(this JsonElement json, string name)
        {
            if (json.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (json.TryGetProperty(name, out var result) &&
                result.ValueKind is not JsonValueKind.Undefined and not JsonValueKind.Null)
            {
                return result;
            }

            return null;
        }

        public static object? GetDynamicOrNull(this JsonElement json) => json.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Number => json.GetDouble(),
            JsonValueKind.String => json.GetString(),
            JsonValueKind.Array => json.EnumerateArray().Select(GetDynamicOrNull).ToArray(),
            JsonValueKind.Object => json.GetDictionaryOrNull(),
            _ => null
        };

        public static string GetStringOrThrow(this JsonElement json) =>
            json.GetString() ?? throw new InvalidOperationException("JSON string is null.");

        public static void WriteDictionaryValue(
            this Utf8JsonWriter writer,
            IEnumerable<KeyValuePair<string, object?>>? dic,
            IDiagnosticLogger? logger)
        {
            if (dic is not null)
            {
                writer.WriteStartObject();

                foreach (var (key, value) in dic)
                {
                    writer.WriteDynamic(key, value, logger);
                }

                writer.WriteEndObject();
            }
            else
            {
                writer.WriteNullValue();
            }
        }

        public static void WriteStringDictionaryValue(
            this Utf8JsonWriter writer,
            IEnumerable<KeyValuePair<string, string?>>? dic)
        {
            if (dic is not null)
            {
                writer.WriteStartObject();

                foreach (var (key, value) in dic)
                {
                    writer.WriteString(key, value);
                }

                writer.WriteEndObject();
            }
            else
            {
                writer.WriteNullValue();
            }
        }

        public static void WriteDictionary(
            this Utf8JsonWriter writer,
            string propertyName,
            IEnumerable<KeyValuePair<string, object?>>? dic,
            IDiagnosticLogger? logger)
        {
            writer.WritePropertyName(propertyName);
            writer.WriteDictionaryValue(dic, logger);
        }

        public static void WriteStringDictionary(
            this Utf8JsonWriter writer,
            string propertyName,
            IEnumerable<KeyValuePair<string, string?>>? dic)
        {
            writer.WritePropertyName(propertyName);
            writer.WriteStringDictionaryValue(dic);
        }

        public static void WriteArrayValue(
            this Utf8JsonWriter writer,
            IEnumerable<object?>? arr,
            IDiagnosticLogger? logger)
        {
            if (arr is not null)
            {
                writer.WriteStartArray();

                foreach (var i in arr)
                {
                    writer.WriteDynamicValue(i, logger);
                }

                writer.WriteEndArray();
            }
            else
            {
                writer.WriteNullValue();
            }
        }

        public static void WriteArray(
            this Utf8JsonWriter writer,
            string propertyName,
            IEnumerable<object?>? arr,
            IDiagnosticLogger? logger)
        {
            writer.WritePropertyName(propertyName);
            writer.WriteArrayValue(arr, logger);
        }

        public static void WriteStringArrayValue(
            this Utf8JsonWriter writer,
            IEnumerable<string?>? arr)
        {
            if (arr is not null)
            {
                writer.WriteStartArray();

                foreach (var i in arr)
                {
                    writer.WriteStringValue(i);
                }

                writer.WriteEndArray();
            }
            else
            {
                writer.WriteNullValue();
            }
        }

        public static void WriteStringArray(
            this Utf8JsonWriter writer,
            string propertyName,
            IEnumerable<string?>? arr)
        {
            writer.WritePropertyName(propertyName);
            writer.WriteStringArrayValue(arr);
        }

        public static void WriteSerializableValue(
            this Utf8JsonWriter writer,
            IJsonSerializable value,
            IDiagnosticLogger? logger)
        {
            value.WriteTo(writer, logger);
        }

        public static void WriteSerializable(
            this Utf8JsonWriter writer,
            string propertyName,
            IJsonSerializable value,
            IDiagnosticLogger? logger)
        {
            writer.WritePropertyName(propertyName);
            writer.WriteSerializableValue(value, logger);
        }

        public static void WriteDynamicValue(
            this Utf8JsonWriter writer,
            object? value,
            IDiagnosticLogger? logger)
        {
            if (value is null)
            {
                writer.WriteNullValue();
            }
            else if (value is IJsonSerializable serializable)
            {
                writer.WriteSerializableValue(serializable, logger);
            }
            else if (value is IEnumerable<KeyValuePair<string, string?>> sdic)
            {
                writer.WriteStringDictionaryValue(sdic);
            }
            else if (value is IEnumerable<KeyValuePair<string, object?>> dic)
            {
                writer.WriteDictionaryValue(dic, logger);
            }
            else if (value is string str)
            {
                writer.WriteStringValue(str);
            }
            else if (value is bool b)
            {
                writer.WriteBooleanValue(b);
            }
            else if (value is int i)
            {
                writer.WriteNumberValue(i);
            }
            else if (value is long l)
            {
                writer.WriteNumberValue(l);
            }
            else if (value is double d)
            {
                writer.WriteNumberValue(d);
            }
            else if (value is DateTime dt)
            {
                writer.WriteStringValue(dt);
            }
            else if (value is DateTimeOffset dto)
            {
                writer.WriteStringValue(dto);
            }
            else if (value is IFormattable formattable)
            {
                writer.WriteStringValue(formattable.ToString(null, CultureInfo.InvariantCulture));
            }
            else
            {
                JsonSerializer.Serialize(writer, value, serializerOption);
            }
        }

        public static void WriteDynamic(
            this Utf8JsonWriter writer,
            string propertyName,
            object? value,
            IDiagnosticLogger? logger)
        {
            writer.WritePropertyName(propertyName);
            var originalPropertyDepth = writer.CurrentDepth;
            try
            {
                writer.WriteDynamicValue(value, logger);
            }
            catch (Exception e)
            {
                // In the event of an instance that can't be serialized, we don't want to throw away a whole event
                // so we'll suppress issues here.
                logger?.LogError(e, "Failed to serialize object for property '{0}'. Original depth: {1}, current depth: {2}",
                    propertyName, originalPropertyDepth, writer.CurrentDepth);

                // The only location in the protocol we allow dynamic objects are Extra and Contexts.
                // Render an empty JSON object instead of null. This allows a round trip where this property name is the
                // key to a map which would otherwise not be set and result in a different object.
                // This affects envelope size which isn't recomputed after a roundtrip.
                if (originalPropertyDepth == writer.CurrentDepth)
                {
                    writer.WriteStartObject();
                }
                while (originalPropertyDepth < writer.CurrentDepth)
                {
                    writer.WriteEndObject();
                }
            }
        }

        public static void WriteBooleanIfNotNull(
            this Utf8JsonWriter writer,
            string propertyName,
            bool? value)
        {
            if (value is not null)
            {
                writer.WriteBoolean(propertyName, value.Value);
            }
        }

        public static void WriteNumberIfNotNull(
            this Utf8JsonWriter writer,
            string propertyName,
            short? value)
        {
            if (value is not null)
            {
                writer.WriteNumber(propertyName, value.Value);
            }
        }

        public static void WriteNumberIfNotNull(
            this Utf8JsonWriter writer,
            string propertyName,
            int? value)
        {
            if (value is not null)
            {
                writer.WriteNumber(propertyName, value.Value);
            }
        }

        public static void WriteNumberIfNotNull(
            this Utf8JsonWriter writer,
            string propertyName,
            long? value)
        {
            if (value is not null)
            {
                writer.WriteNumber(propertyName, value.Value);
            }
        }

        public static void WriteNumberIfNotNull(
            this Utf8JsonWriter writer,
            string propertyName,
            float? value)
        {
            if (value is not null)
            {
                writer.WriteNumber(propertyName, value.Value);
            }
        }

        public static void WriteNumberIfNotZero(
            this Utf8JsonWriter writer,
            string propertyName,
            short value)
        {
            if (value is not 0)
            {
                writer.WriteNumber(propertyName, value);
            }
        }

        public static void WriteNumberIfNotZero(
            this Utf8JsonWriter writer,
            string propertyName,
            int value)
        {
            if (value is not 0)
            {
                writer.WriteNumber(propertyName, value);
            }
        }

        public static void WriteNumberIfNotZero(
            this Utf8JsonWriter writer,
            string propertyName,
            long value)
        {
            if (value is not 0)
            {
                writer.WriteNumber(propertyName, value);
            }
        }

        public static void WriteNumberIfNotZero(
            this Utf8JsonWriter writer,
            string propertyName,
            float value)
        {
            if (value is not 0)
            {
                writer.WriteNumber(propertyName, value);
            }
        }

        public static void WriteNumberIfNotZero(
            this Utf8JsonWriter writer,
            string propertyName,
            double value)
        {
            if (value is not 0)
            {
                writer.WriteNumber(propertyName, value);
            }
        }

        public static void WriteStringIfNotWhiteSpace(
            this Utf8JsonWriter writer,
            string propertyName,
            string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                writer.WriteString(propertyName, value);
            }
        }

        public static void WriteStringIfNotNull(
            this Utf8JsonWriter writer,
            string propertyName,
            DateTimeOffset? value)
        {
            if (value is not null)
            {
                writer.WriteString(propertyName, value.Value);
            }
        }

        public static void WriteSerializableIfNotNull(
            this Utf8JsonWriter writer,
            string propertyName,
            IJsonSerializable? value,
            IDiagnosticLogger? logger)
        {
            if (value is not null)
            {
                writer.WriteSerializable(propertyName, value, logger);
            }
        }

        public static void WriteDictionaryIfNotEmpty(
            this Utf8JsonWriter writer,
            string propertyName,
            IEnumerable<KeyValuePair<string, object?>>? dic,
            IDiagnosticLogger? logger)
        {
            var asDictionary = dic as IReadOnlyDictionary<string, object?> ?? dic?.ToDictionary();
            if (asDictionary is not null && asDictionary.Count > 0)
            {
                writer.WriteDictionary(propertyName, asDictionary, logger);
            }
        }

        public static void WriteStringDictionaryIfNotEmpty(
            this Utf8JsonWriter writer,
            string propertyName,
            IEnumerable<KeyValuePair<string, string?>>? dic)
        {
            var asDictionary = dic as IReadOnlyDictionary<string, string?> ?? dic?.ToDictionary();
            if (asDictionary is not null && asDictionary.Count > 0)
            {
                writer.WriteStringDictionary(propertyName, asDictionary);
            }
        }

        public static void WriteArrayIfNotEmpty(
            this Utf8JsonWriter writer,
            string propertyName,
            IEnumerable<object?>? arr,
            IDiagnosticLogger? logger)
        {
            var asList = arr as IReadOnlyList<object?> ?? arr?.ToArray();
            if (asList is not null && asList.Count > 0)
            {
                writer.WriteArray(propertyName, asList, logger);
            }
        }

        public static void WriteStringArrayIfNotEmpty(
            this Utf8JsonWriter writer,
            string propertyName,
            IEnumerable<string?>? arr)
        {
            var asList = arr as IReadOnlyList<string?> ?? arr?.ToArray();
            if (asList is not null && asList.Count > 0)
            {
                writer.WriteStringArray(propertyName, asList);
            }
        }

        public static void WriteDynamicIfNotNull(
            this Utf8JsonWriter writer,
            string propertyName,
            object? value,
            IDiagnosticLogger? logger)
        {
            if (value is not null)
            {
                writer.WriteDynamic(propertyName, value, logger);
            }
        }

        public static void WriteString(
            this Utf8JsonWriter writer,
            string propertyName,
            Enumeration? value)
        {
            if (value == null)
            {
                writer.WriteNull(propertyName);
            }
            else
            {
                writer.WriteString(propertyName, value.Value);
            }
        }
    }
}
