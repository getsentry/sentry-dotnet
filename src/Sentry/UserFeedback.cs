using Sentry.Extensibility;
using Sentry.Internal.Extensions;

namespace Sentry;

/// <summary>
/// Sentry User Feedback.
/// </summary>
[Obsolete("Use SentryFeedback instead.")]
public sealed class UserFeedback : ISentryJsonSerializable
{
    /// <summary>
    /// The eventId of the event to which the user feedback is associated.
    /// </summary>
    public SentryId EventId { get; }

    /// <summary>
    /// The name of the user.
    /// </summary>
    public string? Name { get; }

    /// <summary>
    /// The name of the user.
    /// </summary>
    public string? Email { get; }

    /// <summary>
    /// Comments of the user about what happened.
    /// </summary>
    public string? Comments { get; }

    /// <summary>
    /// Initializes an instance of <see cref="UserFeedback"/>.
    /// </summary>
    public UserFeedback(SentryId eventId, string? name, string? email, string? comments)
    {
        EventId = eventId;
        Name = name;
        Email = email;
        Comments = comments;
    }

    /// <inheritdoc />
    public void WriteTo(Utf8JsonWriter writer, IDiagnosticLogger? logger)
    {
        writer.WriteStartObject();

        writer.WriteSerializable("event_id", EventId, logger);
        writer.WriteStringIfNotWhiteSpace("name", Name);
        writer.WriteStringIfNotWhiteSpace("email", Email);
        writer.WriteStringIfNotWhiteSpace("comments", Comments);

        writer.WriteEndObject();
    }

    /// <summary>
    /// Parses from JSON.
    /// </summary>
    public static UserFeedback FromJson(JsonElement json)
    {
        var eventId = json.GetPropertyOrNull("event_id")?.Pipe(SentryId.FromJson) ?? SentryId.Empty;
        var name = json.GetPropertyOrNull("name")?.GetString();
        var email = json.GetPropertyOrNull("email")?.GetString();
        var comments = json.GetPropertyOrNull("comments")?.GetString();

        return new UserFeedback(eventId, name, email, comments);
    }
}
