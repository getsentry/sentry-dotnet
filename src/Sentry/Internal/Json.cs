using System.Text.Json;

namespace Sentry.Internal
{
    internal static class Json
    {
        public static JsonElement Parse(byte[] json)
        {
            using var jsonDocument = JsonDocument.Parse(json);
            return jsonDocument.RootElement.Clone();
        }

        public static JsonElement Parse(string json)
        {
            using var jsonDocument = JsonDocument.Parse(json);
            return jsonDocument.RootElement.Clone();
        }
    }
}
