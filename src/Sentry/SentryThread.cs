using Sentry.Extensibility;
using Sentry.Internal.Extensions;

namespace Sentry;

/// <summary>
/// A thread running at the time of an event.
/// </summary>
/// <see href="https://develop.sentry.dev/sdk/event-payloads/threads/"/>
public sealed class SentryThread : ISentryJsonSerializable
{
    /// <summary>
    /// The Id of the thread.
    /// </summary>
    public int? Id { get; set; }

    /// <summary>
    /// The name of the thread.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Whether the crash happened on this thread.
    /// </summary>
    public bool? Crashed { get; set; }

    /// <summary>
    /// An optional flag to indicate that the thread was in the foreground.
    /// </summary>
    public bool? Current { get; set; }

    /// <summary>
    /// Stack trace.
    /// </summary>
    /// <see href="https://develop.sentry.dev/sdk/event-payloads/stacktrace/"/>
    public SentryStackTrace? Stacktrace { get; set; }

    /// <inheritdoc />
    public void WriteTo(Utf8JsonWriter writer, IDiagnosticLogger? logger)
    {
        writer.WriteStartObject();

        writer.WriteNumberIfNotNull("id", Id);
        writer.WriteStringIfNotWhiteSpace("name", Name);
        writer.WriteBooleanIfNotNull("crashed", Crashed);
        writer.WriteBooleanIfNotNull("current", Current);
        writer.WriteSerializableIfNotNull("stacktrace", Stacktrace, logger);

        writer.WriteEndObject();
    }

    /// <summary>
    /// Parses from JSON.
    /// </summary>
    public static SentryThread FromJson(JsonElement json)
    {
        var id = json.GetPropertyOrNull("id")?.GetInt32();
        var name = json.GetPropertyOrNull("name")?.GetString();
        var crashed = json.GetPropertyOrNull("crashed")?.GetBoolean();
        var current = json.GetPropertyOrNull("current")?.GetBoolean();
        var stacktrace = json.GetPropertyOrNull("stacktrace")?.Pipe(SentryStackTrace.FromJson);

        return new SentryThread
        {
            Id = id,
            Name = name,
            Crashed = crashed,
            Current = current,
            Stacktrace = stacktrace
        };
    }
}
