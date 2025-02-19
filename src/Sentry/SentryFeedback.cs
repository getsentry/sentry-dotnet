using Sentry.Extensibility;
using Sentry.Internal.Extensions;

namespace Sentry;

/// <summary>
/// Sentry User Feedback.
/// </summary>
public sealed class SentryFeedback : ISentryJsonSerializable
{
    // final String? replayId;
    // final String? url;
    // final SentryId? associatedEventId;

    /// <summary>
    /// Message containing the user's feedback.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// The name of the user.
    /// </summary>
    public string? ContactEmail { get; }

    /// <summary>
    /// The name of the user.
    /// </summary>
    public string? Name { get; }

    /// <summary>
    /// Optional ID of the Replay session associated with the feedback.
    /// </summary>
    public string? ReplayId { get; }

    /// <summary>
    /// The name of the user.
    /// </summary>
    public string? Url { get; }

    /// <summary>
    /// Optional ID of the event that the user feedback is associated with.
    /// </summary>
    public SentryId AssociatedEventId { get; }

    /// <summary>
    /// Initializes an instance of <see cref="SentryFeedback"/>.
    /// </summary>
    public SentryFeedback(string message, string? contactEmail, string? name, string? replayId, string? url, SentryId eventId)
    {
        Message = message;
        ContactEmail = contactEmail;
        Name = name;
        ReplayId = replayId;
        Url = url;
        AssociatedEventId = eventId;
    }

    /// <inheritdoc />
    public void WriteTo(Utf8JsonWriter writer, IDiagnosticLogger? logger)
    {
        writer.WriteStartObject();

        writer.WriteString("message", Message);
        writer.WriteStringIfNotWhiteSpace("contact_email", ContactEmail);
        writer.WriteStringIfNotWhiteSpace("name", Name);
        writer.WriteStringIfNotWhiteSpace("replay_id", ReplayId);
        writer.WriteStringIfNotWhiteSpace("url", Url);
        writer.WriteSerializable("associated_event_id", AssociatedEventId, logger);

        writer.WriteEndObject();
    }

    /// <summary>
    /// Parses from JSON.
    /// </summary>
    public static SentryFeedback FromJson(JsonElement json)
    {
        var message = json.GetPropertyOrNull("message")?.GetString() ?? "";
        var contactEmail = json.GetPropertyOrNull("contact_email")?.GetString();
        var name = json.GetPropertyOrNull("name")?.GetString();
        var replayId = json.GetPropertyOrNull("replay_id")?.GetString();
        var url = json.GetPropertyOrNull("url")?.GetString();
        var eventId = json.GetPropertyOrNull("associated_event_id")?.Pipe(SentryId.FromJson) ?? SentryId.Empty;

        return new SentryFeedback(message, contactEmail, name, replayId, url, eventId);
    }
}
