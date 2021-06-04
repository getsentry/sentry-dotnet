using System;
using System.Globalization;
using FluentAssertions;
using Sentry.Tests.Helpers;
using Xunit;

namespace Sentry.Tests
{
    public class SessionTests
    {
        [Fact]
        public void Serialization_Session_Success()
        {
            // Arrange
            var session = new Session(
                "foo",
                "bar",
                DateTimeOffset.Parse("2020-01-01", CultureInfo.InvariantCulture),
                "release123",
                "env123",
                "192.168.0.1",
                "Google Chrome"
            );

            session.ReportError();
            session.ReportError();
            session.ReportError();

            session.End(SessionEndStatus.Crashed);

            var sessionUpdate = new SessionUpdate(
                session,
                true,
                DateTimeOffset.Parse("2020-01-02", CultureInfo.InvariantCulture)
            );

            // Act
            var json = sessionUpdate.ToJsonString();

            // Assert
            json.Should().Be(
                "{" +
                "\"sid\":\"foo\"," +
                "\"did\":\"bar\"," +
                "\"init\":true," +
                "\"started\":\"2020-01-01T00:00:00+02:00\"," +
                "\"timestamp\":\"2020-01-02T00:00:00+02:00\"," +
                "\"duration\":86400," +
                "\"errors\":3," +
                "\"status\":\"crashed\"," +
                "\"attrs\":{" +
                "\"release\":\"release123\"," +
                "\"environment\":\"env123\"," +
                "\"ip_address\":\"192.168.0.1\"," +
                "\"user_agent\":\"Google Chrome\"" +
                "}" +
                "}"
            );
        }
    }
}
