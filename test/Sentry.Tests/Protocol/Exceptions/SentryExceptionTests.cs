using System.Collections.Generic;
using Sentry.Protocol;
using Xunit;

namespace Sentry.Tests.Protocol.Exceptions
{
    public class SentryExceptionTests
    {
        [Fact]
        public void SerializeObject_AllPropertiesSetToNonDefault_SerializesValidObject()
        {
            var sut = new SentryException
            {
                Value = "Value",
                Type = "Type",
                Module = "Module",
                ThreadId = 1,
                Stacktrace = new SentryStackTrace
                {
                    Frames = { new SentryStackFrame
                    {
                        FileName = "FileName"
                    }}
                },
                Data = { new KeyValuePair<string, object>("data-key", "data-value") },
                Mechanism = new Mechanism
                {
                    Description = "Description"
                }
            };

            var actual = sut.ToJsonString();

            Assert.Equal(
                "{\"type\":\"Type\"," +
                "\"value\":\"Value\"," +
                "\"module\":\"Module\"," +
                "\"thread_id\":1," +
                "\"stacktrace\":{\"frames\":[{\"filename\":\"FileName\"}]}," +
                "\"mechanism\":{\"description\":\"Description\"}}",
                actual
            );
        }

        [Fact]
        public void Data_Getter_NotNull()
        {
            var sut = new SentryException();
            Assert.NotNull(sut.Data);
        }
    }
}
