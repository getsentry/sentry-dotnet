using System;
using System.Text.Json;
using Sentry.Internal.Extensions;

namespace Sentry
{
    /// <summary>
    /// Session update.
    /// </summary>
    // https://develop.sentry.dev/sdk/sessions/#session-update-payload
    public class SessionUpdate : ISession, IJsonSerializable
    {
        /// <inheritdoc />
        public SentryId Id { get; }

        /// <inheritdoc />
        public string? DistinctId { get; }

        /// <inheritdoc />
        public DateTimeOffset StartTimestamp { get; }

        /// <inheritdoc />
        public string Release { get; }

        /// <inheritdoc />
        public string? Environment { get; }

        /// <inheritdoc />
        public string? IpAddress { get; }

        /// <inheritdoc />
        public string? UserAgent { get; }

        /// <inheritdoc />
        public SessionEndStatus? EndStatus { get; }

        /// <inheritdoc />
        public int ErrorCount { get; }

        /// <summary>
        /// Whether this is the initial update.
        /// </summary>
        public bool IsInitial { get; }

        /// <summary>
        /// Timestamp.
        /// </summary>
        public DateTimeOffset Timestamp { get; }

        /// <summary>
        /// Sequence number.
        /// </summary>
        public int SequenceNumber { get; }

        /// <summary>
        /// Duration of time since the start of the session.
        /// </summary>
        public TimeSpan Duration => Timestamp - StartTimestamp;

        /// <summary>
        /// Initializes a new instance of <see cref="SessionUpdate"/>.
        /// </summary>
        public SessionUpdate(
            SentryId id,
            string? distinctId,
            DateTimeOffset startTimestamp,
            string release,
            string? environment,
            string? ipAddress,
            string? userAgent,
            SessionEndStatus? endStatus,
            int errorCount,
            bool isInitial,
            DateTimeOffset timestamp,
            int sequenceNumber)
        {
            Id = id;
            DistinctId = distinctId;
            StartTimestamp = startTimestamp;
            Release = release;
            Environment = environment;
            IpAddress = ipAddress;
            UserAgent = userAgent;
            EndStatus = endStatus;
            ErrorCount = errorCount;
            IsInitial = isInitial;
            Timestamp = timestamp;
            SequenceNumber = sequenceNumber;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="SessionUpdate"/>.
        /// </summary>
        public SessionUpdate(ISession session, bool isInitial, DateTimeOffset timestamp, int sequenceNumber)
            : this(
                session.Id,
                session.DistinctId,
                session.StartTimestamp,
                session.Release,
                session.Environment,
                session.IpAddress,
                session.UserAgent,
                session.EndStatus,
                session.ErrorCount,
                isInitial,
                timestamp,
                sequenceNumber)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="SessionUpdate"/>.
        /// </summary>
        public SessionUpdate(SessionUpdate sessionUpdate, bool isInitial)
            : this(sessionUpdate, isInitial, sessionUpdate.Timestamp, sessionUpdate.SequenceNumber)
        {
        }

        /// <inheritdoc />
        public void WriteTo(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();

            writer.WriteSerializable("sid", Id);

            if (!string.IsNullOrWhiteSpace(DistinctId))
            {
                writer.WriteString("did", DistinctId);
            }

            writer.WriteBoolean("init", IsInitial);

            writer.WriteString("started", StartTimestamp);

            writer.WriteString("timestamp", Timestamp);

            writer.WriteNumber("seq", SequenceNumber);

            writer.WriteNumber("duration", (int)Duration.TotalSeconds);

            writer.WriteNumber("errors", ErrorCount);

            // State
            if (EndStatus is { } endState)
            {
                writer.WriteString("status", endState.ToString().ToSnakeCase());
            }

            // Attributes
            writer.WriteStartObject("attrs");

            writer.WriteString("release", Release);

            if (!string.IsNullOrWhiteSpace(Environment))
            {
                writer.WriteString("environment", Environment);
            }

            if (!string.IsNullOrWhiteSpace(IpAddress))
            {
                writer.WriteString("ip_address", IpAddress);
            }

            if (!string.IsNullOrWhiteSpace(UserAgent))
            {
                writer.WriteString("user_agent", UserAgent);
            }

            writer.WriteEndObject();

            writer.WriteEndObject();
        }

        /// <summary>
        /// Parses <see cref="SessionUpdate"/> from JSON.
        /// </summary>
        public static SessionUpdate FromJson(JsonElement json)
        {
            var id = json.GetProperty("sid").GetStringOrThrow().Pipe(SentryId.Parse);
            var distinctId = json.GetPropertyOrNull("did")?.GetString();
            var startTimestamp = json.GetProperty("started").GetDateTimeOffset();
            var release = json.GetProperty("attrs").GetProperty("release").GetStringOrThrow();
            var environment = json.GetProperty("attrs").GetPropertyOrNull("environment")?.GetString();
            var ipAddress = json.GetProperty("attrs").GetPropertyOrNull("ip_address")?.GetString();
            var userAgent = json.GetProperty("attrs").GetPropertyOrNull("user_agent")?.GetString();
            var endStatus = json.GetPropertyOrNull("status")?.GetString()?.ParseEnum<SessionEndStatus>();
            var errorCount = json.GetPropertyOrNull("errors")?.GetInt32() ?? 0;
            var isInitial = json.GetPropertyOrNull("init")?.GetBoolean() ?? false;
            var timestamp = json.GetProperty("timestamp").GetDateTimeOffset();
            var sequenceNumber = json.GetProperty("seq").GetInt32();

            return new SessionUpdate(
                id,
                distinctId,
                startTimestamp,
                release,
                environment,
                ipAddress,
                userAgent,
                endStatus,
                errorCount,
                isInitial,
                timestamp,
                sequenceNumber
            );
        }
    }
}
