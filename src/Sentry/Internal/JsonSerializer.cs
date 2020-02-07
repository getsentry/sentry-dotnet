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
            Converters = new[] { StringEnumConverter },
            DateFormatHandling = DateFormatHandling.IsoDateFormat
        };

        public static string SerializeObject<T>(T @object) => JsonConvert.SerializeObject(@object, Settings);
        public static dynamic DeserializeObject(string json) => JsonConvert.DeserializeObject(json);
#if DEBUG
        public static T DeserializeObject<T>(string json) => JsonConvert.DeserializeObject<T>(json);
#endif
    }
}
