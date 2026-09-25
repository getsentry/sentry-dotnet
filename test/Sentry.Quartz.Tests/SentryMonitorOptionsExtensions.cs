namespace Sentry.Quartz.Tests;

/// <summary>
/// Serializes a <see cref="SentryMonitorOptions"/> to JSON so its private schedule/crontab state - only observable
/// through <see cref="ISentryJsonSerializable.WriteTo"/> - can be asserted on in tests.
/// </summary>
internal static class SentryMonitorOptionsExtensions
{
    public static JsonDocument ToJsonDocument(this SentryMonitorOptions options)
    {
        // WriteTo starts by writing the "monitor_config" property, so - like when a check-in is serialized - it
        // needs to be called from within an already-open JSON object.
        using var buffer = new MemoryStream();
        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();
            options.WriteTo(writer, logger: null);
            writer.WriteEndObject();
        }

        buffer.Seek(0, SeekOrigin.Begin);
        return JsonDocument.Parse(buffer);
    }
}
