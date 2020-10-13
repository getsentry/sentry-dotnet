using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Sentry.Internal
{
    internal static class JsonSerializer
    {
        private static readonly StringEnumConverter StringEnumConverter = new StringEnumConverter();

        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
            NullValueHandling = NullValueHandling.Ignore,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            Formatting = Formatting.None,
            Converters = new JsonConverter[] { StringEnumConverter },
            DateFormatHandling = DateFormatHandling.IsoDateFormat
        };

        public static string SerializeObject(object obj) => JsonConvert.SerializeObject(obj, Settings);

        public static async Task SerializeObjectAsync(object obj, Stream stream, CancellationToken cancellationToken = default)
        {
            using var textWriter = new StreamWriter(stream, EncodingEx.Utf8WithoutBom, 1024, true);
            using var jsonWriter = new JsonTextWriter(textWriter);

            await jsonWriter.WriteValueAsync(obj, cancellationToken).ConfigureAwait(false);
        }
    }
}
