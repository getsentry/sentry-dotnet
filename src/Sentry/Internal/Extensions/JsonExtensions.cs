using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;

namespace Sentry.Internal.Extensions
{
    internal static class JsonExtensions
    {
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
            IEnumerable<KeyValuePair<string, object?>>? dic)
        {
            if (dic is not null)
            {
                writer.WriteStartObject();

                foreach (var (key, value) in dic)
                {
                    writer.WriteDynamic(key, value);
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
            IEnumerable<KeyValuePair<string, object?>>? dic)
        {
            writer.WritePropertyName(propertyName);
            writer.WriteDictionaryValue(dic);
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
            IEnumerable<object?>? arr)
        {
            if (arr is not null)
            {
                writer.WriteStartArray();

                foreach (var i in arr)
                {
                    writer.WriteDynamicValue(i);
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
            IEnumerable<object?>? arr)
        {
            writer.WritePropertyName(propertyName);
            writer.WriteArrayValue(arr);
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
            IJsonSerializable value)
        {
            value.WriteTo(writer);
        }

        public static void WriteSerializable(
            this Utf8JsonWriter writer,
            string propertyName,
            IJsonSerializable value)
        {
            writer.WritePropertyName(propertyName);
            writer.WriteSerializableValue(value);
        }

        public static void WriteDynamicValue(
            this Utf8JsonWriter writer,
            object? value)
        {
            if (value is null)
            {
                writer.WriteNullValue();
            }
            else if (value is IJsonSerializable serializable)
            {
                writer.WriteSerializableValue(serializable);
            }
            else if (value is IEnumerable<KeyValuePair<string, string?>> sdic)
            {
                writer.WriteStringDictionaryValue(sdic);
            }
            else if (value is IEnumerable<KeyValuePair<string, object?>> dic)
            {
                writer.WriteDictionaryValue(dic);
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
                JsonSerializer.Serialize(writer, value);
            }
        }

        public static void WriteDynamic(
            this Utf8JsonWriter writer,
            string propertyName,
            object? value)
        {
            writer.WritePropertyName(propertyName);
            writer.WriteDynamicValue(value);
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
            IJsonSerializable? value)
        {
            if (value is not null)
            {
                writer.WriteSerializable(propertyName, value);
            }
        }

        public static void WriteDictionaryIfNotEmpty(
            this Utf8JsonWriter writer,
            string propertyName,
            IEnumerable<KeyValuePair<string, object?>>? dic)
        {
            var asDictionary = dic as IReadOnlyDictionary<string, object?> ?? dic?.ToDictionary();
            if (asDictionary is not null && asDictionary.Count > 0)
            {
                writer.WriteDictionary(propertyName, asDictionary);
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
            IEnumerable<object?>? arr)
        {
            var asList = arr as IReadOnlyList<object?> ?? arr?.ToArray();
            if (asList is not null && asList.Count > 0)
            {
                writer.WriteArray(propertyName, asList);
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
            object? value)
        {
            if (value is not null)
            {
                writer.WriteDynamic(propertyName, value);
            }
        }
    }
}
