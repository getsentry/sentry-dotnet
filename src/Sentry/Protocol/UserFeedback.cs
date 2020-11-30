using System.Text.Json;
using Sentry.Internal.Extensions;
using Sentry.Protocol;

namespace Sentry
{
    /// <summary>
    /// Sentry User Feedback.
    /// </summary>
    public sealed class UserFeedback : IJsonSerializable
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
        public string Email { get; }

        /// <summary>
        /// Comments of the user about what happened.
        /// </summary>
        public string Comments { get; }

        /// <summary>
        /// Initializes an instance of <see cref="UserFeedback"/>.
        /// </summary>
        public UserFeedback(SentryId eventId, string email, string comments, string? name = null)
        {
            EventId = eventId;
            Name = name;
            Email = email;
            Comments = comments;
        }

        /// <inheritdoc />
        public void WriteTo(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();

            // Event ID
            writer.WriteSerializable("event_id", EventId);

            // Name
            if (!string.IsNullOrWhiteSpace(Name))
            {
                writer.WriteString("name", Name);
            }

            // Email
            if (!string.IsNullOrWhiteSpace(Email))
            {
                writer.WriteString("email", Email);
            }

            // Comments
            if (!string.IsNullOrWhiteSpace(Comments))
            {
                writer.WriteString("comments", Comments);
            }

            writer.WriteEndObject();
        }

        /// <summary>
        /// Parses from JSON.
        /// </summary>
        public static UserFeedback FromJson(JsonElement json)
        {
            var eventId = json.GetPropertyOrNull("event_id")?.Pipe(SentryId.FromJson) ?? SentryId.Empty;
            var name = json.GetProperty("name").GetStringOrThrow();
            var email = json.GetProperty("email").GetStringOrThrow();
            var comments = json.GetProperty("comments").GetStringOrThrow();

            return new UserFeedback(eventId, email, comments, name);
        }
    }
}
