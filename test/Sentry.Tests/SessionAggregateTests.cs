using System;
using System.Globalization;
using System.Text.Json;
using FluentAssertions;
using Sentry.Tests.Helpers;
using Xunit;

namespace Sentry.Tests
{
    public class SessionAggregateTests
    {
        [Fact]
        public void Serialization_ValidAggregate_Success()
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

        [Fact]
        public void FromJson_ValidAggregate_Success()
        {
            // Arrange
            var expectedDate = DateTimeOffset.Parse("2020-01-01T00:00:00+00:00", CultureInfo.InvariantCulture);
            var json =
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
                "}";
            var jsonElement = JsonDocument.Parse(json).RootElement;

            // Act
            var aggregate = SessionAggregate.FromJson(jsonElement);

            // Assert
            Assert.Equal(expectedDate, aggregate.StartTimestamp);
            Assert.Equal(1, aggregate.ExitedCount);
            Assert.Equal(3, aggregate.ErroredCount);
            Assert.Equal("release123", aggregate.Release);
            Assert.Equal("env123", aggregate.Environment);
        }

        [Fact]
        public void FromJson_MissingAttr_Errored()
        {
            // Arrange
            var json =
                "{" +
                "\"aggregates\":" +
                "[" +
                "{" +
                "\"started\":\"2020-01-01T00:00:00+00:00\"," +
                "\"exited\":1," +
                "\"errored\":3" +
                "}" +
                "]" +
                "}";
            var jsonElement = JsonDocument.Parse(json).RootElement;

            // Act
            try
            {
                var aggregate = SessionAggregate.FromJson(jsonElement);

            }
            // Assert
            catch (MissingMethodException me)
            {
                Assert.Equal(SessionAggregate.MissingRequiredKeysMessage, me.Message);
            }
        }

        [Fact]
        public void FromJson_MissingRelease_Errored()
        {
            // Arrange
            var json =
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
                "\"environment\":\"env123\"" +
                "}" +
                "}";
            var jsonElement = JsonDocument.Parse(json).RootElement;

            // Act
            try
            {
                 var aggregate = SessionAggregate.FromJson(jsonElement);

            }
            // Assert
            catch (MissingMethodException me)
            {
                Assert.Equal(SessionAggregate.MissingRequiredKeysMessage, me.Message);
            }
        }

        [Fact]
        public void FromJson_MissingAggregates_Errored()
        {
            // Arrange
            var json =
                "{" +
                "\"attrs\":{" +
                "\"release\":\"release123\"," +
                "\"environment\":\"env123\"" +
                "}" +
                "}";
            var jsonElement = JsonDocument.Parse(json).RootElement;

            // Act
            try
            {
                var aggregate = SessionAggregate.FromJson(jsonElement);

            }
            // Assert
            catch (MissingMethodException me)
            {
                Assert.Equal(SessionAggregate.MissingRequiredKeysMessage, me.Message);
            }
        }
    }
}
