using System;
using System.Text.Json;
using Sentry.Internal.Extensions;

namespace Sentry
{
    /// <summary>
    /// Snapshot of a session.
    /// </summary>
    public class SessionUpdate : IJsonSerializable
    {
        /// <summary>
        /// Session.
        /// </summary>
        public Session Session { get; }

        /// <summary>
        /// Whether this is the initial snapshot.
        /// </summary>
        public bool IsInitial { get; }

        /// <summary>
        /// Timestamp.
        /// </summary>
        public DateTimeOffset Timestamp { get; }

        /// <summary>
        /// Duration of time since start of the session.
        /// </summary>
        public TimeSpan Duration => Timestamp - Session.Timestamp;

        internal SessionUpdate(Session session, bool isInitial, DateTimeOffset timestamp)
        {
            Session = session;
            IsInitial = isInitial;
            Timestamp = timestamp;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="SessionUpdate"/>.
        /// </summary>
        public SessionUpdate(Session session, bool isInitial)
            : this(session, isInitial, DateTimeOffset.Now)
        {
        }

        /// <inheritdoc />
        public void WriteTo(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();

            writer.WriteString("sid", Session.Id);

            if (!string.IsNullOrWhiteSpace(Session.DistinctId))
            {
                writer.WriteString("did", Session.DistinctId);
            }

            if (IsInitial)
            {
                writer.WriteBoolean("init", IsInitial);
            }

            writer.WriteString("started", Session.Timestamp);

            writer.WriteString("timestamp", Timestamp);

            writer.WriteNumber("duration", (int)Duration.TotalSeconds);

            writer.WriteNumber("errors", Session.ErrorCount);

            // State
            if (Session.EndStatus is { } endState)
            {
                writer.WriteString("status", endState.ToString().ToSnakeCase());
            }

            // Attributes
            writer.WriteStartObject("attrs");

            writer.WriteString("release", Session.Release);

            if (!string.IsNullOrWhiteSpace(Session.Environment))
            {
                writer.WriteString("environment", Session.Environment);
            }

            if (!string.IsNullOrWhiteSpace(Session.IpAddress))
            {
                writer.WriteString("ip_address", Session.IpAddress);
            }

            if (!string.IsNullOrWhiteSpace(Session.UserAgent))
            {
                writer.WriteString("user_agent", Session.UserAgent);
            }

            writer.WriteEndObject();

            writer.WriteEndObject();
        }

        /// <summary>
        /// Parses <see cref="SessionUpdate"/> from JSON.
        /// </summary>
        public static SessionUpdate FromJson(JsonElement json)
        {
            var id = json.GetProperty("id").GetStringOrThrow();
            var distinctId = json.GetPropertyOrNull("did")?.GetString();
            var timestamp = json.GetProperty("started").GetDateTimeOffset();
            var release = json.GetProperty("attrs").GetProperty("release").GetStringOrThrow();
            var environment = json.GetProperty("attrs").GetPropertyOrNull("environment")?.GetString();
            var ipAddress = json.GetProperty("attrs").GetPropertyOrNull("ip_address")?.GetString();
            var userAgent = json.GetProperty("attrs").GetPropertyOrNull("user_agent")?.GetString();

            var isInitial = json.GetPropertyOrNull("init")?.GetBoolean() ?? false;
            var updateTimestamp = json.GetProperty("timestamp").GetDateTimeOffset();

            var session = new Session(
                id,
                distinctId,
                timestamp,
                release,
                environment,
                ipAddress,
                userAgent
            );

            return new SessionUpdate(session, isInitial, updateTimestamp);
        }
    }
}
