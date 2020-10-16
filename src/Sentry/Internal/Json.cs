using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

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
            DateFormatHandling = DateFormatHandling.IsoDateFormat
        };

        private static JsonTextWriter CreateWriter(Stream stream) => new JsonTextWriter(
            new StreamWriter(stream, Encoding, 1024, true)
        );

        public static void SerializeToStream(object obj, Stream stream)
        {
            using var writer = CreateWriter(stream);
            Serializer.Serialize(writer, obj);
        }

        public static byte[] SerializeToByteArray(object obj)
        {
            using var buffer = new MemoryStream();
            SerializeToStream(obj, buffer);

            return buffer.ToArray();
        }

        public static string Serialize(object obj) => Encoding.GetString(
            SerializeToByteArray(obj)
        );

        public static async Task SerializeToStreamAsync(
            object obj,
            Stream stream,
            CancellationToken cancellationToken = default)
        {
            using var writer = CreateWriter(stream);
            Serializer.Serialize(writer, obj);
            await writer.FlushAsync(cancellationToken);
        }
    }
}
