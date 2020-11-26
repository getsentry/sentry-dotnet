using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using JsonSerializer = Newtonsoft.Json.JsonSerializer;

namespace Sentry.Internal
{
    internal static class Json
    {
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

        [return: MaybeNull]
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

        [return: MaybeNull]
        public static T DeserializeFromByteArray<T>(byte[] data)
        {
            using var buffer = new MemoryStream(data);
            return DeserializeFromStream<T>(buffer);
        }

        public static string Serialize(object obj) =>
            Encoding.GetString(SerializeToByteArray(obj));

        [return: MaybeNull]
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

        public static JsonElement Parse(string json)
        {
            using var jsonDocument = JsonDocument.Parse(json);
            return jsonDocument.RootElement.Clone();
        }
    }
}
