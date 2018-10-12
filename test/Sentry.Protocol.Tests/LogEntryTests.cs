using System.Collections.Generic;
using Xunit;

namespace Sentry.Protocol.Tests
{
    public class LogEntryTests
    {
        [Fact]
        public void SerializeObject_AllPropertiesSetToNonDefault_SerializesValidObject()
        {
            var sut = new LogEntry
            {
                Message = "Message {eventId} {name}",
                Params = new object[] { 100, "test-name" },
                Formatted = "Message 100 test-name"
            };

            var actual = JsonSerializer.SerializeObject(sut);

            Assert.Equal("{\"message\":\"Message {eventId} {name}\","
                        + "\"params\":[100,\"test-name\"],"
                        + "\"formatted\":\"Message 100 test-name\"}",
                    actual);
        }

        [Theory]
        [MemberData(nameof(TestCases))]
        public void SerializeObject_TestCase_SerializesAsExpected((LogEntry msg, string serialized) @case)
        {
            var actual = JsonSerializer.SerializeObject(@case.msg);

            Assert.Equal(@case.serialized, actual);
        }

        public static IEnumerable<object[]> TestCases()
        {
            yield return new object[] { (new LogEntry(), "{}") };
            yield return new object[] { (new LogEntry { Message = "some message" }, "{\"message\":\"some message\"}") };
            yield return new object[] { (new LogEntry { Params = new[] { "param" } }, "{\"params\":[\"param\"]}") };
            yield return new object[] { (new LogEntry { Formatted = "some formatted" }, "{\"formatted\":\"some formatted\"}") };
        }
    }
}
