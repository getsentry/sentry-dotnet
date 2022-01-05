using System.Text.Json;

internal static class JsonSerializableExtensions
{
    public static string ToJsonString(this IJsonSerializable serializable)
    {
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        writer.WriteSerializableValue(serializable, new TraceDiagnosticLogger(SentryLevel.Debug));
        writer.Flush();

        return Encoding.UTF8.GetString(stream.ToArray());
    }
}
