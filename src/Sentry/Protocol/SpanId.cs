using System;
using System.Linq;
using System.Text;
using System.Text.Json;
using Sentry.Internal.Extensions;

namespace Sentry.Protocol
{
    /// <summary>
    /// Sentry span ID.
    /// </summary>
    public readonly struct SpanId : IEquatable<SpanId>, IJsonSerializable
    {
        private static readonly Random Random = new();

        private static readonly char[] AllowedChars =
        {
            '0', '1', '2', '3', '4', '5', '6', '7', '8', '9',
            'a', 'b', 'c', 'd', 'e', 'f'
        };

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

        /// <summary>
        /// Generates a new Sentry ID.
        /// </summary>
        public static SpanId Create()
        {
            var buffer = new StringBuilder(16);
            for (var i = 0; i < buffer.Length; i++)
            {
                buffer[i] = AllowedChars[Random.Next(0, AllowedChars.Length)];
            }

            return new(buffer.ToString());
        }

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
                ? new SpanId(value)
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
        /// The <see cref="string"/> from the <see cref="SentryId"/>.
        /// </summary>
        public static implicit operator string(SpanId id) => id._value;

        /// <summary>
        /// A <see cref="SentryId"/> from a <see cref="string"/>.
        /// </summary>
        public static implicit operator SpanId(string value) => new(value);
    }
}
