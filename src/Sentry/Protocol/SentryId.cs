using System;
using System.Text.Json;

namespace Sentry.Protocol
{
    /// <summary>
    /// The identifier of an event in Sentry.
    /// </summary>
    public readonly struct SentryId : IEquatable<SentryId>, IJsonSerializable
    {
        private readonly Guid _guid;

        /// <summary>
        /// An empty sentry id.
        /// </summary>
        public static readonly SentryId Empty = Guid.Empty;

        /// <summary>
        /// Creates a new instance of a Sentry Id.
        /// </summary>
        public SentryId(Guid guid) => _guid = guid;

        /// <summary>
        /// Sentry Id in the format Sentry recognizes.
        /// </summary>
        /// <remarks>
        /// Default <see cref="ToString"/> of <see cref="Guid"/> includes
        /// dashes which sentry doesn't expect when searching events.
        /// </remarks>
        /// <returns>String representation of the event id.</returns>
        public override string ToString() => _guid.ToString("n");

        // Note: spans are sentry IDs with only 16 characters, rest being truncated.
        // This is obviously a bad idea as it invalidates GUID's uniqueness properties
        // (https://devblogs.microsoft.com/oldnewthing/20080627-00/?p=21823)
        // but all other SDKs do it this way, so we have no choice but to comply.
        /// <summary>
        /// Returns a truncated ID.
        /// </summary>
        public string ToShortString() => ToString().Substring(0, 16);

        /// <inheritdoc />
        public bool Equals(SentryId other) => _guid.Equals(other._guid);

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is SentryId other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() => _guid.GetHashCode();

        /// <summary>
        /// Generates a new Sentry ID.
        /// </summary>
        public static SentryId Create() => new SentryId(Guid.NewGuid());

        /// <inheritdoc />
        public void WriteTo(Utf8JsonWriter writer) => writer.WriteStringValue(ToString());

        public static SentryId Parse(string value) => new SentryId(Guid.Parse(value));

        /// <summary>
        /// Parses from JSON.
        /// </summary>
        public static SentryId FromJson(JsonElement json)
        {
            var id = json.GetString();

            return !string.IsNullOrWhiteSpace(id)
                ? new SentryId(Guid.Parse(id))
                : Empty;
        }

        /// <summary>
        /// Equality operator.
        /// </summary>
        public static bool operator ==(SentryId left, SentryId right) => left.Equals(right);

        /// <summary>
        /// Equality operator.
        /// </summary>
        public static bool operator !=(SentryId left, SentryId right) => !(left == right);

        /// <summary>
        /// The <see cref="Guid"/> from the <see cref="SentryId"/>.
        /// </summary>
        public static implicit operator Guid(SentryId sentryId) => sentryId._guid;

        /// <summary>
        /// A <see cref="SentryId"/> from a <see cref="Guid"/>.
        /// </summary>
        public static implicit operator SentryId(Guid guid) => new SentryId(guid);
    }
}
