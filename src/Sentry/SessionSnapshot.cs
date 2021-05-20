using System;
using System.Text.Json;

namespace Sentry
{
    internal class SessionSnapshot : IJsonSerializable
    {
        private readonly Session _session;
        private readonly bool _isInitial;

        public SessionSnapshot(Session session, bool isInitial)
        {
            _session = session;
            _isInitial = isInitial;
        }

        public void WriteTo(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();

            writer.WriteString("sid", _session.Id);

            if (!string.IsNullOrWhiteSpace(_session.DistinctId))
            {
                writer.WriteString("did", _session.DistinctId);
            }

            if (_isInitial)
            {
                writer.WriteBoolean("init", _isInitial);
            }

            writer.WriteString("timestamp", _session.Timestamp);

            writer.WriteNumber("errors", _session.ErrorCount);

            // Attributes
            writer.WriteStartObject("attrs");

            writer.WriteString("release", _session.Release);

            if (!string.IsNullOrWhiteSpace(_session.Environment))
            {
                writer.WriteString("environment", _session.Environment);
            }

            if (!string.IsNullOrWhiteSpace(_session.IpAddress))
            {
                writer.WriteString("ip_address", _session.IpAddress);
            }

            if (!string.IsNullOrWhiteSpace(_session.UserAgent))
            {
                writer.WriteString("user_agent", _session.UserAgent);
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
