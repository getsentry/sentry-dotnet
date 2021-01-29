using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using Sentry.Internal.Extensions;

namespace Sentry
{
    internal interface IJsonSerializable
    {
        /// <summary>
        /// Writes the object as JSON.
        /// </summary>
        /// <remarks>
        /// Note: this method is meant only for internal use and is exposed due to a language limitation.
        /// Avoid relying on this method in user code.
        /// </remarks>
        void WriteTo(Utf8JsonWriter writer);
    }

    internal static class JsonSerializableExtensions
    {
        public static string ToJsonString(this IJsonSerializable serializable)
        {
            using var stream = new MemoryStream();
            using var writer = new Utf8JsonWriter(stream);

            writer.WriteSerializableValue(serializable);
            writer.Flush();

            return Encoding.UTF8.GetString(stream.ToArray());
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
                writer.WriteDictionaryValue(sdic);
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
                writer.WriteStringValue(value.ToString());
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
    }
}
