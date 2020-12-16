using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Sentry.Protocol;

namespace Sentry.Internal.Extensions
{
    // TODO: refactor this mess
    internal static class JsonExtensions
    {
        public static void Deconstruct(this JsonProperty jsonProperty, out string name, out JsonElement value)
        {
            name = jsonProperty.Name;
            value = jsonProperty.Value;
        }

        public static void WriteDictionaryValue(
            this Utf8JsonWriter writer,
            IEnumerable<KeyValuePair<string, object?>>? dic)
        {
            if (dic != null)
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

        public static void WriteDictionaryValue(
            this Utf8JsonWriter writer,
            IEnumerable<KeyValuePair<string, string?>>? dic)
        {
            if (dic != null)
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

        public static void WriteDictionary(
            this Utf8JsonWriter writer,
            string propertyName,
            IEnumerable<KeyValuePair<string, string?>>? dic)
        {
            writer.WritePropertyName(propertyName);
            writer.WriteDictionaryValue(dic);
        }

        public static IReadOnlyDictionary<string, object?>? GetObjectDictionary(this JsonElement json)
        {
            if (json.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            var result = new Dictionary<string, object?>();

            foreach (var (name, value) in json.EnumerateObject())
            {
                result[name] = value.GetDynamic();
            }

            return result;
        }

        public static IReadOnlyDictionary<string, string?>? GetDictionary(this JsonElement json)
        {
            if (json.ValueKind == JsonValueKind.Null)
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

            if (json.TryGetProperty(name, out var result))
            {
                if (json.ValueKind == JsonValueKind.Undefined ||
                    json.ValueKind == JsonValueKind.Null)
                {
                    return null;
                }

                return result;
            }

            return null;
        }

        public static TOut Pipe<TIn, TOut>(this TIn input, Func<TIn, TOut> pipe) => pipe(input);

        public static object? GetDynamic(this JsonElement json) => json.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Number => json.GetDouble(),
            JsonValueKind.String => json.GetString(),
            JsonValueKind.Array => json.EnumerateArray().Select(GetDynamic).ToArray(),
            JsonValueKind.Object => json.GetObjectDictionary(),
            _ => null
        };

        public static string GetStringOrThrow(this JsonElement json) =>
            json.GetString() ?? throw new InvalidOperationException("JSON string is null.");
    }
}
