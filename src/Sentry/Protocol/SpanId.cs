using System;
using System.Text.Json;

namespace Sentry.Protocol
{
    /// <summary>
    /// Sentry span ID.
    /// </summary>
    public readonly struct SpanId : IEquatable<SpanId>, IJsonSerializable
    {
        private readonly string _value;

        /// <summary>
        /// An empty Sentry span ID.
        /// </summary>
        public static readonly SpanId Empty = new("0000000000000000");

        /// <summary>
        /// Creates a new instance of a Sentry span Id.
        /// </summary>
        public SpanId(string value) => _value = value;

        /// <inheritdoc />
        public bool Equals(SpanId other) => StringComparer.Ordinal.Equals(_value, other._value);

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is SpanId other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(_value);

        /// <inheritdoc />
        public override string ToString() => _value;

        // Note: spans are sentry IDs with only 16 characters, rest being truncated.
        // This is obviously a bad idea as it invalidates GUID's uniqueness properties
        // (https://devblogs.microsoft.com/oldnewthing/20080627-00/?p=21823)
        // but all other SDKs do it this way, so we have no choice but to comply.
        /// <summary>
        /// Generates a new Sentry ID.
        /// </summary>
        public static SpanId Create() => new(Guid.NewGuid().ToString("n").Substring(0, 16));

        /// <inheritdoc />
        public void WriteTo(Utf8JsonWriter writer) => writer.WriteStringValue(_value);

        /// <summary>
        /// Parses from string.
        /// </summary>
        public static SpanId Parse(string value) => new(value);

        /// <summary>
        /// Parses from JSON.
        /// </summary>
        public static SpanId FromJson(JsonElement json)
        {
            var value = json.GetString();

            return !string.IsNullOrWhiteSpace(value)
                ? Parse(value)
                : Empty;
        }

        /// <summary>
        /// Equality operator.
        /// </summary>
        public static bool operator ==(SpanId left, SpanId right) => left.Equals(right);

        /// <summary>
        /// Equality operator.
        /// </summary>
        public static bool operator !=(SpanId left, SpanId right) => !(left == right);

        /// <summary>
        /// The <see cref="Guid"/> from the <see cref="SentryId"/>.
        /// </summary>
        public static implicit operator string(SpanId id) => id._value;

        /// <summary>
        /// A <see cref="SentryId"/> from a <see cref="Guid"/>.
        /// </summary>
        public static implicit operator SpanId(string value) => new(value);
    }
}
