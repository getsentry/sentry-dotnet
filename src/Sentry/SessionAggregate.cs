using System;
using System.Linq;
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
        internal const string MissingRequiredKeysMessage = "Failed to deserialize an aggregated session due to one or more required properties were missing";
        /// <summary>
        /// Timestamp of the group, rounded down to the minute.
        /// </summary>
        public DateTimeOffset StartTimestamp { get; }

        /// <summary>
        /// The number of sessions with status "exited" without any errors.
        /// </summary>
        public int ExitedCount { get; }

        /// <summary>
        /// The number of sessions with status "exited" that had a non-zero errors count.
        /// </summary>
        public int ErroredCount { get; }

        /// <summary>
        /// Release.
        /// </summary>
        public string Release { get; }

        /// <summary>
        /// Environment.
        /// </summary>
        public string? Environment { get; }

        internal SessionAggregate(
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
            if (json.TryGetProperty("aggregates", out var aggregateArray) &&
                aggregateArray.EnumerateArray().FirstOrDefault() is { } aggregate &&
                json.TryGetProperty("attrs", out var attributes) &&
                attributes.TryGetProperty("release", out var releaseProperty) &&
                releaseProperty.GetString() is { } release)
            {
                var dateStarted = aggregate.GetProperty("started").GetDateTimeOffset();
                var exited = aggregate.GetProperty("exited").GetInt32();
                var errored = aggregate.GetProperty("errored").GetInt32();

                var environment = attributes.GetProperty("environment").GetString();
                return new SessionAggregate(dateStarted, exited, errored, release, environment);
            }

            // Aggregate is missing one or more required properties, throw exception?
            throw new MissingMethodException(MissingRequiredKeysMessage);
        }
    }
}
