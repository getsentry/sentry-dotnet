using System;
using System.Text.Json;
using Sentry.Internal.Extensions;

namespace Sentry
{
    /// <summary>
    /// Session aggregate.
    /// </summary>
    // https://develop.sentry.dev/sdk/sessions/#session-aggregates-payload
    // (Sentry's payload has multiple aggregates, but we can simplify it to only having one)
    public class SessionAggregate : IJsonSerializable
    {
        public DateTimeOffset StartTimestamp { get; }

        public int ExitedCount { get; }

        public int ErroredCount { get; }

        /// <summary>
        /// Release.
        /// </summary>
        public string Release { get; }

        /// <summary>
        /// Environment.
        /// </summary>
        public string? Environment { get; }

        public SessionAggregate(
            DateTimeOffset startTimestamp,
            int exitedCount,
            int erroredCount,
            string release,
            string? environment)
        {
            StartTimestamp = startTimestamp;
            ExitedCount = exitedCount;
            ErroredCount = erroredCount;
            Release = release;
            Environment = environment;
        }

        /// <inheritdoc />
        public void WriteTo(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();

            writer.WriteStartArray("aggregates");
            writer.WriteStartObject();

            writer.WriteString("started", StartTimestamp);
            writer.WriteNumber("exited", ExitedCount);
            writer.WriteNumber("errored", ErroredCount);

            writer.WriteEndObject();
            writer.WriteEndArray();

            // Attributes
            writer.WriteStartObject("attrs");

            writer.WriteString("release", Release);
            writer.WriteStringIfNotWhiteSpace("environment", Environment);

            writer.WriteEndObject();

            writer.WriteEndObject();
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
