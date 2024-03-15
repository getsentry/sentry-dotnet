using Sentry.Extensibility;
using Sentry.Internal.Extensions;

namespace Sentry;

internal class PersistedSessionUpdate : ISentryJsonSerializable
{
    public SessionUpdate Update { get; }

    public DateTimeOffset? PauseTimestamp { get; }

    public PersistedSessionUpdate(SessionUpdate update, DateTimeOffset? pauseTimestamp)
    {
        Update = update;
        PauseTimestamp = pauseTimestamp;
    }

    public void WriteTo(Utf8JsonWriter writer, IDiagnosticLogger? logger)
    {
        writer.WriteStartObject();

        writer.WriteSerializable("update", Update, logger);

        if (PauseTimestamp is { } pauseTimestamp)
        {
            writer.WriteString("paused", pauseTimestamp);
        }

        writer.WriteEndObject();
    }

    public static PersistedSessionUpdate FromJson(JsonElement json)
    {
        var update = SessionUpdate.FromJson(json.GetProperty("update"));
        var pauseTimestamp = json.GetPropertyOrNull("paused")?.GetDateTimeOffset();

        return new PersistedSessionUpdate(update, pauseTimestamp);
    }
}
