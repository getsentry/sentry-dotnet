using System.Collections.Generic;
using Sentry.Protocol;
using Xunit;

namespace Sentry.Tests.Protocol
{
    public class SentryThreadTests
    {
        [Fact]
        public void SerializeObject_AllPropertiesSetToNonDefault_SerializesValidObject()
        {
            var sut = new SentryThread
            {
                Crashed = true,
                Current = true,
                Id = 0,
                Name = "thread11",
                Stacktrace = new SentryStackTrace
                {
                    Frames = { new SentryStackFrame
                    {
                        FileName = "test"
                    }}
                }
            };

            var actual = sut.ToJsonString();

            Assert.Equal(
                "{\"id\":0," +
                "\"name\":\"thread11\"," +
                "\"crashed\":true," +
                "\"current\":true," +
                "\"stacktrace\":{\"frames\":[{\"filename\":\"test\"}]}}",
                actual
            );
        }

        [Theory]
        [MemberData(nameof(TestCases))]
        public void SerializeObject_TestCase_SerializesAsExpected((SentryThread thread, string serialized) @case)
        {
            var actual = @case.thread.ToJsonString();

            Assert.Equal(@case.serialized, actual);
        }

        public static IEnumerable<object[]> TestCases()
        {
            yield return new object[] { (new SentryThread(), "{}") };
            yield return new object[] { (new SentryThread { Name = "some name" }, "{\"name\":\"some name\"}") };
            yield return new object[] { (new SentryThread { Crashed = false }, "{\"crashed\":false}") };
            yield return new object[] { (new SentryThread { Current = false }, "{\"current\":false}") };
            yield return new object[] { (new SentryThread { Id = 200 }, "{\"id\":200}") };
            yield return new object[] { (new SentryThread { Stacktrace = new SentryStackTrace { Frames = { new SentryStackFrame { InApp = true } } } }
                , "{\"stacktrace\":{\"frames\":[{\"in_app\":true}]}}") };
        }
    }
}
