using System.Text.Json;

namespace Sentry.Internal;

internal static class Json
{
    public static T Parse<T>(byte[] json, Func<JsonElement, T> factory)
    {
        using var jsonDocument = JsonDocument.Parse(json);
        return factory.Invoke(jsonDocument.RootElement);
    }

    public static T Parse<T>(string json, Func<JsonElement, T> factory)
    {
        using var jsonDocument = JsonDocument.Parse(json);
        return factory.Invoke(jsonDocument.RootElement);
    }

    public static T Load<T>(string filePath, Func<JsonElement, T> factory)
    {
        using var file = File.OpenRead(filePath);
        using var jsonDocument = JsonDocument.Parse(file);
        return factory.Invoke(jsonDocument.RootElement);
    }
}
