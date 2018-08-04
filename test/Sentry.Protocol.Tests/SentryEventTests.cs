using System;
using Xunit;

namespace Sentry.Protocol.Tests
{
    public class SentryEventTests
    {
        [Fact]
        public void SerializeObject_AllPropertiesSetToNonDefault_SerializesValidObject()
        {
            var ex = new Exception("exception message");
            var timestamp = DateTimeOffset.MaxValue;
            var id = Guid.Parse("4b780f4c-ec03-42a7-8ef8-a41c9d5621f8");
            var sut = new SentryEvent(ex, timestamp, id)
            {
                User = new User { Id = "user-id" },
                Request = new Request { Method = "POST" },
                Contexts = new Contexts { ["context_key"] = "context_value" },
                Sdk = new SdkVersion { Name = "SDK-test" },
                Culprit = "culprit",
                Environment = "environment",
                Level = SentryLevel.Fatal,
                Logger = "logger",
                Message = "message",
                StructuredMessage = new SentryMessage
                {
                    Message = "structured_message"
                },
                Modules = { {"module_key", "module_value"}},
                Release = "release",
                SentryExceptions = new[] { new SentryException { Value = "exception_value" } },
                SentryThreads = new[] { new SentryThread { Crashed = true } },
                ServerName = "server_name",
                Transaction = "transaction"
            };

            sut.AddBreadcrumb(timestamp, "crumb");
            sut.SetExtra("extra_key", "extra_value");
            sut.SetFingerprint(new[] { "fingerprint" });
            sut.SetTag("tag_key", "tag_value");

            var actual = JsonSerializer.SerializeObject(sut);

            Assert.Equal("{\"modules\":{\"module_key\":\"module_value\"}," +
                         "\"event_id\":\"4b780f4cec0342a78ef8a41c9d5621f8\"," +
                         "\"timestamp\":\"9999-12-31T23:59:59.9999999+00:00\"," +
                         "\"message\":\"message\"," +
                         "\"logentry\":{\"message\":\"structured_message\"}," +
                         "\"logger\":\"logger\"," +
                         "\"platform\":\"csharp\"," +
                         "\"level\":\"fatal\"," +
                         "\"culprit\":\"culprit\"," +
                         "\"server_name\":\"server_name\"," +
                         "\"release\":\"release\"," +
                         "\"exception\":{\"values\":[{\"value\":\"exception_value\"}]}," +
                         "\"threads\":{\"values\":[{\"crashed\":true}]}," +
                         "\"user\":{\"id\":\"user-id\"}," +
                         "\"contexts\":{\"context_key\":\"context_value\"}," +
                         "\"request\":{\"method\":\"POST\"}," +
                         "\"fingerprint\":[\"fingerprint\"]," +
                         "\"breadcrumbs\":[{\"timestamp\":\"9999-12-31T23:59:59Z\",\"message\":\"crumb\"}]," +
                         "\"extra\":{\"extra_key\":\"extra_value\"},\"tags\":{\"tag_key\":\"tag_value\"}," +
                         "\"transaction\":\"transaction\"," +
                         "\"environment\":\"environment\"," +
                         "\"sdk\":{\"name\":\"SDK-test\"}}",
                actual);
        }

        [Fact]
        public void Ctor_Platform_CSharp()
        {
            var evt = new SentryEvent();
            Assert.Equal(Constants.Platform, evt.Platform);
        }

        [Fact]
        public void Ctor_Timestamp_NonDefault()
        {
            var evt = new SentryEvent();
            Assert.NotEqual(default, evt.Timestamp);
        }

        [Fact]
        public void Ctor_EventId_NonDefault()
        {
            var evt = new SentryEvent();
            Assert.NotEqual(default, evt.EventId);
        }

        [Fact]
        public void Ctor_Exception_Stored()
        {
            var e = new Exception();
            var evt = new SentryEvent(e);
            Assert.Same(e, evt.Exception);
        }
    }
}
