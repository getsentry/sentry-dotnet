using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Sentry.Protocol.Tests
{
    internal class JsonSerializer
    {
        private static readonly StringEnumConverter StringEnumConverter = new StringEnumConverter();
        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
            NullValueHandling = NullValueHandling.Ignore,
            Formatting = Formatting.None,
            Converters = new[] { StringEnumConverter }
        };

        public static string SerializeObject<T>(T @object) => JsonConvert.SerializeObject(@object, Settings);
        public static dynamic DeserializeObject(string json) => JsonConvert.DeserializeObject(json);
    }
}
