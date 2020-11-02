using System;
using System.IO;
using Sentry.Internal;
using Sentry.Protocol;
using Xunit;

namespace Sentry.Tests.Protocol
{
    public class UserFeedbackTests
    {
        [Fact]
        public void Serialization_SentryUserFeedbacks_Success()
        {
            // Arrange
            var eventId = new SentryId(Guid.Parse("acbe351c61494e7b807fd7e82a435ffc"));
            var userFeedback = new UserFeedback(eventId, "myEmail@service.com", "my comment", "myName");
            using var stream = new MemoryStream();

            // Act
            var serializedContent = Json.Serialize(userFeedback);

            // Assert
            var assertExpected = "{\"event_id\":\"acbe351c61494e7b807fd7e82a435ffc\",\"name\":\"myName\",\"email\":\"myEmail@service.com\",\"comments\":\"my comment\"}";
            Assert.Equal(assertExpected, serializedContent);
        }
    }
}
