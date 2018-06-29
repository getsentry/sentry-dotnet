using Sentry.Internal;
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
                Debug = SentryLevel.Debug,
                Info = SentryLevel.Info,
                Warning = SentryLevel.Warning,
                Error = SentryLevel.Error,
                Fatal = SentryLevel.Fatal,
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
