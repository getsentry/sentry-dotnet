using System;
using System.Globalization;
using FluentAssertions;
using Sentry.Tests.Helpers;
using Xunit;

namespace Sentry.Tests
{
    public class SessionAggregateTests
    {

        [Fact]
        public void Serialization_Session_Success()
        {
            // Arrange
            var session = new SessionAggregate(
                DateTimeOffset.Parse("2020-01-01T00:00:00+00:00", CultureInfo.InvariantCulture),
                1,
                3,
                "release123",
                "env123"
            );

            // Act
            var json = session.ToJsonString();

            // Assert
            json.Should().Be(
                "{" +
                "\"aggregates\":" +
                "[" +
                "{" +
                "\"started\":\"2020-01-01T00:00:00+00:00\"," +
                "\"exited\":1," +
                "\"errored\":3" +
                "}" +
                "]," +
                "\"attrs\":{" +
                "\"release\":\"release123\"," +
                "\"environment\":\"env123\"" +
                "}" +
                "}"
            );
        }
    }
}
