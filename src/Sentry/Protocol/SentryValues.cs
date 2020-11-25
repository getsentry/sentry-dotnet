using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Sentry.Protocol
{
    /// <summary>
    /// Helps serialization of Sentry protocol types which include a values property.
    /// </summary>
    public class SentryValues<T> : IJsonSerializable
    {
        /// <summary>
        /// The values.
        /// </summary>
        public IEnumerable<T> Values { get; }

        /// <summary>
        /// Creates an instance from the specified <see cref="IEnumerable{T}"/>.
        /// </summary>
        public SentryValues(IEnumerable<T>? values) => Values = values ?? Enumerable.Empty<T>();

        public void WriteTo(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();

            writer.WriteStartArray("values");

            foreach (var i in Values)
            {
                writer.WriteDynamicValue(i);
            }

            writer.WriteEndArray();

            writer.WriteEndObject();
        }
    }
}
