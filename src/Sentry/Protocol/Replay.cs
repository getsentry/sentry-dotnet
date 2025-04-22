using Sentry.Extensibility;
using Sentry.Internal;
using Sentry.Internal.Extensions;

namespace Sentry.Protocol;

/// <summary>
/// Sentry Replay context interface.
/// </summary>
/// <example>
/// {
///     "contexts": {
///         "replay": {
///             "replay_id": "12312012123120121231201212312012"
///         }
///     }
/// }
/// </example>
/// <see href="https://develop.sentry.dev/sdk/data-model/event-payloads/contexts/#replay-context"/>
public sealed class Replay : ISentryJsonSerializable, ICloneable<Replay>, IUpdatable<Replay>
{
    /// <summary>
    /// Tells Sentry which type of context this is.
    /// </summary>
    public const string Type = "replay";

    /// <summary>
    /// The name of the runtime.
    /// </summary>
    public SentryId? ReplayId { get; set; }

    /// <summary>
    /// Clones this instance.
    /// </summary>
    public Replay Clone()
    {
        var response = new Replay();

        response.UpdateFrom(this);

        return response;
    }

    /// <summary>
    /// Updates this instance with data from the properties in the <paramref name="source"/>,
    /// unless there is already a value in the existing property.
    /// </summary>
    public void UpdateFrom(Replay source)
    {
        ReplayId ??= source.ReplayId;
    }

    /// <summary>
    /// Updates this instance with data from the properties in the <paramref name="source"/>,
    /// unless there is already a value in the existing property.
    /// </summary>
    public void UpdateFrom(object source)
    {
        if (source is Replay response)
        {
            UpdateFrom(response);
        }
    }

    /// <inheritdoc />
    public void WriteTo(Utf8JsonWriter writer, IDiagnosticLogger? logger)
    {
        writer.WriteStartObject();

        writer.WriteString("type", Type);
        writer.WriteSerializableIfNotNull("replay_id", ReplayId, logger);

        writer.WriteEndObject();
    }

    /// <summary>
    /// Parses from JSON.
    /// </summary>
    public static Replay FromJson(JsonElement json)
    {
        var replayId = json.GetPropertyOrNull("replay_id")?.Pipe(SentryId.FromJson);

        return new Replay
        {
            ReplayId = replayId
        };
    }
}
