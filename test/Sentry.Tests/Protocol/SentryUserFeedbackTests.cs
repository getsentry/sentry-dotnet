using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Sentry.Protocol;
using Xunit;

namespace Sentry.Tests.Protocol
{
    public class SentryUserFeedbackTests
    {
        [Fact]
        public async Task Serialization_SentryUserFeedbacks_Success()
        {
            // Arrange
            var eventId = new SentryId(Guid.Parse("acbe351c61494e7b807fd7e82a435ffc"));
            var userFeedback = new SentryUserFeedback(eventId, "myName", "myEmail@service.com", "my comment");
            using var stream = new MemoryStream();

            // Act
            await userFeedback.SerializeAsync(stream, default);
            var serializedContent = Encoding.UTF8.GetString(stream.ToArray());

            // Assert
            var assertExpected = "{\"event_id\":\"acbe351c61494e7b807fd7e82a435ffc\",\"name\":\"myName\",\"email\":\"myEmail@service.com\",\"comments\":\"my comment\"}";
            Assert.Equal(assertExpected, serializedContent);
        }
    }
}
