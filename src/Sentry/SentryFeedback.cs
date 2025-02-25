using Sentry.Extensibility;
using Sentry.Internal;
using Sentry.Internal.Extensions;

namespace Sentry;

/// <summary>
/// Sentry User Feedback.
/// </summary>
public sealed class SentryFeedback : ISentryJsonSerializable, ICloneable<SentryFeedback>
{
    /// <summary>
    /// Tells Sentry which type of context this is.
    /// </summary>
    internal const string Type = "feedback";

    /// <summary>
    /// Message containing the user's feedback.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// The name of the user.
    /// </summary>
    public string? ContactEmail { get; set; }

    /// <summary>
    /// The name of the user.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Optional ID of the Replay session associated with the feedback.
    /// </summary>
    public string? ReplayId { get; set; }

    /// <summary>
    /// The name of the user.
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// Optional ID of the event that the user feedback is associated with.
    /// </summary>
    public SentryId? AssociatedEventId { get; set; }

    /// <summary>
    /// Creates an instance of <see cref="SentryFeedback"/>.
    /// </summary>
    public SentryFeedback(string message, string? contactEmail = null, string? name = null, string? replayId = null, string? url = null, SentryId? associatedEventId = null)
    {
        Message = message;
        ContactEmail = contactEmail;
        Name = name;
        ReplayId = replayId;
        Url = url;
        AssociatedEventId = associatedEventId;
    }

    /// <inheritdoc />
    public void WriteTo(Utf8JsonWriter writer, IDiagnosticLogger? logger)
    {
        if (string.IsNullOrEmpty(Message))
        {
            logger?.LogWarning("Feedback message is empty - serializing as null");
            writer.WriteNullValue();
            return;
        }

        writer.WriteStartObject();

        writer.WriteString("message", Message);
        writer.WriteStringIfNotWhiteSpace("contact_email", ContactEmail);
        writer.WriteStringIfNotWhiteSpace("name", Name);
        writer.WriteStringIfNotWhiteSpace("replay_id", ReplayId);
        writer.WriteStringIfNotWhiteSpace("url", Url);
        writer.WriteSerializableIfNotNull("associated_event_id", AssociatedEventId, logger);

        writer.WriteEndObject();
    }

    /// <summary>
    /// Parses from JSON.
    /// </summary>
    public static SentryFeedback FromJson(JsonElement json)
    {
        var message = json.GetPropertyOrNull("message")?.GetString() ?? "<empty>";
        var contactEmail = json.GetPropertyOrNull("contact_email")?.GetString();
        var name = json.GetPropertyOrNull("name")?.GetString();
        var replayId = json.GetPropertyOrNull("replay_id")?.GetString();
        var url = json.GetPropertyOrNull("url")?.GetString();
        var eventId = json.GetPropertyOrNull("associated_event_id")?.Pipe(SentryId.FromJson);

        return new SentryFeedback(message, contactEmail, name, replayId, url, eventId);
    }

    internal SentryFeedback Clone() => ((ICloneable<SentryFeedback>)this).Clone();

    SentryFeedback ICloneable<SentryFeedback>.Clone()
        => new(Message, ContactEmail, Name, ReplayId, Url, AssociatedEventId);
}
