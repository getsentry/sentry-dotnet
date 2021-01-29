using System;
using System.Text.Json;

namespace Sentry
{
    /// <summary>
    /// Sentry span ID.
    /// </summary>
    public readonly struct SpanId : IEquatable<SpanId>, IJsonSerializable
    {
        private readonly string _value;

        private const string EmptyValue = "0000000000000000";

        /// <summary>
        /// An empty Sentry span ID.
        /// </summary>
        public static readonly SpanId Empty = new(EmptyValue);

        /// <summary>
        /// Creates a new instance of a Sentry span Id.
        /// </summary>
        public SpanId(string value) => _value = value;

        // This method is used to return a string with all zeroes in case
        // the `_value` is equal to null.
        // It can be equal to null because this is a struct and always has
        // a parameterless constructor that evaluates to an instance with
        // all fields initialized to default values.
        // Effectively, using this method instead of just referencing `_value`
        // makes the behavior more consistent, for example:
        // default(SpanId).ToString() -> "0000000000000000"
        // default(SpanId) == SpanId.Empty -> true
        private string GetNormalizedValue() => !string.IsNullOrWhiteSpace(_value)
            ? _value
            : EmptyValue;

        /// <inheritdoc />
        public bool Equals(SpanId other) => StringComparer.Ordinal.Equals(
            GetNormalizedValue(),
            other.GetNormalizedValue()
        );

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is SpanId other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(GetNormalizedValue());

        /// <inheritdoc />
        public override string ToString() => GetNormalizedValue();

        // Note: spans are sentry IDs with only 16 characters, rest being truncated.
        // This is obviously a bad idea as it invalidates GUID's uniqueness properties
        // (https://devblogs.microsoft.com/oldnewthing/20080627-00/?p=21823)
        // but all other SDKs do it this way, so we have no choice but to comply.
        /// <summary>
        /// Generates a new Sentry ID.
        /// </summary>
        public static SpanId Create() => new(Guid.NewGuid().ToString("n").Substring(0, 16));

        /// <inheritdoc />
        public void WriteTo(Utf8JsonWriter writer) => writer.WriteStringValue(GetNormalizedValue());

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
        public static implicit operator string(SpanId id) => id.ToString();

        // Note: no implicit conversion from `string` to `SpanId` as that leads to serious bugs.
        // For example, given a method:
        // transaction.StartChild(SpanId parentSpanId, string operation)
        // And an *extension* method:
        // transaction.StartChild(string operation, string description)
        // The following code:
        // transaction.StartChild("foo", "bar")
        // Will resolve to the first method and not the second, which is incorrect.

        /*
        /// <summary>
        /// A <see cref="SentryId"/> from a <see cref="Guid"/>.
        /// </summary>
        public static implicit operator SpanId(string value) => new(value);
        */
    }
}
