using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace Sentry.Internal
{
    [AttributeUsage(AttributeTargets.Property)]
    internal class DontSerializeEmptyAttribute : Attribute {}

    internal static class Json
    {
        private class ContractResolver : DefaultContractResolver
        {
            protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
            {
                var jsonProperty = base.CreateProperty(member, memberSerialization);
                var property = jsonProperty.DeclaringType.GetProperty(jsonProperty.UnderlyingName);

                // DontSerializeEmpty
                if (jsonProperty.ShouldSerialize is null &&
                    property?.GetCustomAttribute<DontSerializeEmptyAttribute>() is {})
                {
                    // Collections
                    if (property.PropertyType.GetInterfaces().Any(i => i == typeof(IEnumerable)))
                    {
                        jsonProperty.ShouldSerialize = o =>
                        {
                            if (property.GetValue(o) is IEnumerable value)
                            {
                                return !value.Cast<object>().Any();
                            }

                            return true;
                        };
                    }
                }

                return jsonProperty;
            }
        }

        private static readonly Encoding Encoding = new UTF8Encoding(false, true);
        private static readonly StringEnumConverter StringEnumConverter = new StringEnumConverter();

        private static readonly JsonSerializer Serializer = new JsonSerializer
        {
            ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
            NullValueHandling = NullValueHandling.Ignore,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            Formatting = Formatting.None,
            Converters = {StringEnumConverter},
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            ContractResolver = new ContractResolver()
        };

        private static JsonTextWriter CreateWriter(Stream stream) => new JsonTextWriter(
            new StreamWriter(stream, Encoding, 1024, true)
        );

        private static JsonTextReader CreateReader(Stream stream) => new JsonTextReader(
            new StreamReader(stream, Encoding, false, 1024, true)
        );

        public static void SerializeToStream(object obj, Stream stream)
        {
            using var writer = CreateWriter(stream);
            Serializer.Serialize(writer, obj);
        }

        public static T DeserializeFromStream<T>(Stream stream)
        {
            using var reader = CreateReader(stream);
            return Serializer.Deserialize<T>(reader);
        }

        public static byte[] SerializeToByteArray(object obj)
        {
            using var buffer = new MemoryStream();
            SerializeToStream(obj, buffer);

            return buffer.ToArray();
        }

        public static T DeserializeFromByteArray<T>(byte[] data)
        {
            using var buffer = new MemoryStream(data);
            return DeserializeFromStream<T>(buffer);
        }

        public static string Serialize(object obj) =>
            Encoding.GetString(SerializeToByteArray(obj));

        public static T Deserialize<T>(string json) =>
            DeserializeFromByteArray<T>(Encoding.GetBytes(json));

        public static async Task SerializeToStreamAsync(
            object obj,
            Stream stream,
            CancellationToken cancellationToken = default)
        {
            using var writer = CreateWriter(stream);
            Serializer.Serialize(writer, obj);
            await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
