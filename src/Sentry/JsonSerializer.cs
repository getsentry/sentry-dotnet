using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Sentry
{
    internal static class JsonSerializer
    {
        private static readonly StringEnumConverter StringEnumConverter = new StringEnumConverter();

        public static string SerializeObject<T>(T @object) => JsonConvert.SerializeObject(@object, StringEnumConverter);
    }
}
