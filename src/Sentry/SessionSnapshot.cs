using System;
using System.Text.Json;

namespace Sentry
{
    public class SessionSnapshot : IJsonSerializable
    {
        public Session Session { get; }

        public bool IsInitial { get; }

        public DateTimeOffset Timestamp { get; }

        public TimeSpan Duration => Timestamp - Session.Timestamp;

        internal SessionSnapshot(Session session, bool isInitial, DateTimeOffset timestamp)
        {
            Session = session;
            IsInitial = isInitial;
            Timestamp = timestamp;
        }

        public SessionSnapshot(Session session, bool isInitial)
            : this(session, isInitial, DateTimeOffset.Now)
        {
        }

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

            writer.WriteString("timestamp", Session.Timestamp);

            writer.WriteNumber("duration", (int)Duration.TotalSeconds);

            writer.WriteNumber("errors", Session.ErrorCount);

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

        public static SessionSnapshot FromJson(JsonElement json)
        {
            throw new NotImplementedException();
        }
    }
}
