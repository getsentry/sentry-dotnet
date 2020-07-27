using Sentry.Internal;
using Sentry.Protocol;
using Xunit;

namespace Sentry.Tests
{
    public class SentryLevelTests
    {
        [Fact]
        public void SerializeObject_CorrectCasing()
        {
            var sut = new
            {
                SentryLevel.Debug,
                SentryLevel.Info,
                SentryLevel.Warning,
                SentryLevel.Error,
                SentryLevel.Fatal,
            };

            var actual = JsonSerializer.SerializeObject(sut);

            Assert.Equal("{\"Debug\":\"debug\","
                        + "\"Info\":\"info\","
                        + "\"Warning\":\"warning\","
                        + "\"Error\":\"error\","
                        + "\"Fatal\":\"fatal\"}",
                    actual);
        }
    }
}
