using System.Text.Json;

namespace Sentry
{
    /// <summary>
    /// Session aggregate.
    /// </summary>
    // https://develop.sentry.dev/sdk/sessions/#session-aggregates-payload
    public class SessionAggregate : IJsonSerializable
    {
        /// <summary>
        /// Release.
        /// </summary>
        public string Release { get; }

        /// <summary>
        /// Environment.
        /// </summary>
        public string? Environment { get; }

        /// <inheritdoc />
        public void WriteTo(Utf8JsonWriter writer)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Parses <see cref="SessionAggregate"/> from JSON.
        /// </summary>
        public static SessionAggregate FromJson(JsonElement json)
        {
            throw new System.NotImplementedException();
        }
    }
}
