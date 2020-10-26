using System.IO;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Sentry.Internal;
using Sentry.Protocol;
using ISerializable = Sentry.Protocol.ISerializable;

namespace Sentry
{
    /// <summary>
    /// Sentry User Feedback.
    /// </summary>
    [DataContract]
    public class SentryUserFeedback : ISerializable
    {

        [DataMember(Name = "event_id", EmitDefaultValue = false)]
        private string _serializableEventId => EventId.ToString();

        /// <summary>
        /// The eventId of the event to which the user feedback is associated.
        /// </summary>
        public SentryId EventId { get; }

        /// <summary>
        /// The name of the user.
        /// </summary>
        [DataMember(Name = "name", EmitDefaultValue = false)]
        public string? Name { get; }

        /// <summary>
        /// The name of the user.
        /// </summary>
        [DataMember(Name = "email", EmitDefaultValue = false)]
        public string Email { get; }

        /// <summary>
        /// Comments of the user about what happened.
        /// </summary>
        [DataMember(Name = "comments", EmitDefaultValue = false)]
        public string Comments { get; }

        /// <summary>
        /// Initializes an instance of <see cref="SentryUserFeedback"/>.
        /// </summary>
        public SentryUserFeedback(SentryId eventId, string email, string comments, string? name = null)
        {
            EventId = eventId;
            Name = name;
            Email = email;
            Comments = comments;
        }

        /// <inheritdoc />
        public async Task SerializeAsync(Stream stream, CancellationToken cancellationToken = default) =>
        await Json.SerializeToStreamAsync(this, stream, cancellationToken).ConfigureAwait(false);
    }
}
