using Sentry.Extensibility;
using Sentry.Internal.Extensions;

namespace Sentry;

internal class PersistedSessionUpdate : ISentryJsonSerializable
{
    public SessionUpdate Update { get; }

    public DateTimeOffset? PauseTimestamp { get; }

    public bool PendingUnhandled { get; }

    public PersistedSessionUpdate(SessionUpdate update, DateTimeOffset? pauseTimestamp, bool pendingUnhandled = false)
    {
        Update = update;
        PauseTimestamp = pauseTimestamp;
        PendingUnhandled = pendingUnhandled;
    }

    public void WriteTo(Utf8JsonWriter writer, IDiagnosticLogger? logger)
    {
        writer.WriteStartObject();

        writer.WriteSerializable("update", Update, logger);

        if (PauseTimestamp is { } pauseTimestamp)
        {
            writer.WriteString("paused", pauseTimestamp);
        }

        if (PendingUnhandled)
        {
            writer.WriteBoolean("pendingUnhandled", PendingUnhandled);
        }

        writer.WriteEndObject();
    }

    public static PersistedSessionUpdate FromJson(JsonElement json)
    {
        var update = SessionUpdate.FromJson(json.GetProperty("update"));
        var pauseTimestamp = json.GetPropertyOrNull("paused")?.GetDateTimeOffset();
        var pendingUnhandled = json.GetPropertyOrNull("pendingUnhandled")?.GetBoolean() ?? false;

        return new PersistedSessionUpdate(update, pauseTimestamp, pendingUnhandled);
    }
}
