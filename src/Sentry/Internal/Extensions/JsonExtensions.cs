using System.Collections.Generic;
using System.Text.Json;

namespace Sentry.Internal.Extensions
{
    internal static class JsonExtensions
    {
        public static void WriteDictionary(
            this Utf8JsonWriter writer,
            string propertyName,
            IReadOnlyDictionary<string, string?>? dic)
        {
            writer.WritePropertyName(propertyName);

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
    }
}
