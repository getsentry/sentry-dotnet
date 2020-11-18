using System;
using System.Collections.Generic;
using Sentry.Protocol;
using Xunit;

namespace Sentry.Tests.Protocol
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
                Environment = "environment",
                Level = SentryLevel.Fatal,
                Logger = "logger",
                Message = new SentryMessage
                {
                    Message = "message",
                    Formatted = "structured_message"
                },
                Modules = { { "module_key", "module_value" } },
                Release = "release",
                SentryExceptions = new[] { new SentryException { Value = "exception_value" } },
                SentryThreads = new[] { new SentryThread { Crashed = true } },
                ServerName = "server_name",
                Transaction = "transaction"
            };

            sut.AddBreadcrumb(new Breadcrumb(timestamp, "crumb"));
            sut.SetExtra("extra_key", "extra_value");
            sut.Fingerprint = new[] {"fingerprint"};
            sut.SetTag("tag_key", "tag_value");

            var actual = JsonSerializer.SerializeObject(sut);

            Assert.Equal(
                "{\"request\":{\"method\":\"POST\"}," +
                "\"contexts\":{\"context_key\":\"context_value\"}," +
                "\"user\":{\"id\":\"user-id\"}," +
                "\"modules\":{\"module_key\":\"module_value\"}," +
                "\"event_id\":\"4b780f4cec0342a78ef8a41c9d5621f8\"," +
                "\"timestamp\":\"9999-12-31T23:59:59.9999999+00:00\"," +
                "\"logentry\":{\"message\":\"message\",\"formatted\":\"structured_message\"}," +
                "\"logger\":\"logger\"," +
                "\"platform\":\"csharp\"," +
                "\"server_name\":\"server_name\"," +
                "\"release\":\"release\"," +
                "\"exception\":{\"values\":[{\"value\":\"exception_value\"}]}," +
                "\"threads\":{\"values\":[{\"crashed\":true}]}," +
                "\"level\":\"fatal\"," +
                "\"transaction\":\"transaction\"," +
                "\"environment\":\"environment\"," +
                "\"sdk\":{\"name\":\"SDK-test\"}," +
                "\"fingerprint\":[\"fingerprint\"]," +
                "\"breadcrumbs\":[{\"timestamp\":\"9999-12-31T23:59:59Z\",\"message\":\"crumb\"}]," +
                "\"extra\":{\"extra_key\":\"extra_value\"}," +
                "\"tags\":{\"tag_key\":\"tag_value\"}}",
                actual
            );
        }

        [Fact]
        public void SerializeObject_NetFxInstallationIsSetToContext_SerializesValidObject()
        {
            var ex = new Exception("exception message");
            var netFxInstallations = new Dictionary<string, string>
            {
                [".NET Framework"] = "\"v2.0.50727\", \"v3.0\", \"v3.5\"",
                [".NET Framework Client"] = "\"v4.8\", \"v4.0.0.0\"",
                [".NET Framework Full"] = "\"v4.8\""

            };
            var timestamp = DateTimeOffset.MaxValue;
            var id = Guid.Parse("4b780f4c-ec03-42a7-8ef8-a41c9d5621f8");
            var sut = new SentryEvent(ex, timestamp, id)
            {
                User = new User { Id = "user-id" },
                Request = new Request { Method = "POST" },
                Contexts = new Contexts { [".NET Framework"] = netFxInstallations },
                Sdk = new SdkVersion { Name = "SDK-test" },
                Environment = "environment",
                Level = SentryLevel.Fatal,
                Logger = "logger",
                Message = new SentryMessage
                {
                    Message = "message",
                    Formatted = "structured_message"
                },
                Modules = { { "module_key", "module_value" } },
                Release = "release",
                SentryExceptions = new[] { new SentryException { Value = "exception_value" } },
                SentryThreads = new[] { new SentryThread { Crashed = true } },
                ServerName = "server_name",
                Transaction = "transaction"
            };

            sut.AddBreadcrumb(new Breadcrumb(timestamp, "crumb"));
            sut.SetExtra("extra_key", "extra_value");
            sut.Fingerprint = new[] {"fingerprint"};
            sut.SetTag("tag_key", "tag_value");

            var actual = JsonSerializer.SerializeObject(sut);

            Assert.Equal(
                "{\"request\":{\"method\":\"POST\"}," +
                "\"contexts\":{\".NET Framework\":{\".NET Framework\":\"\\\"v2.0.50727\\\", \\\"v3.0\\\", \\\"v3.5\\\"\",\".NET Framework Client\":\"\\\"v4.8\\\", \\\"v4.0.0.0\\\"\",\".NET Framework Full\":\"\\\"v4.8\\\"\"}}," +
                "\"user\":{\"id\":\"user-id\"}," +
                "\"modules\":{\"module_key\":\"module_value\"}," +
                "\"event_id\":\"4b780f4cec0342a78ef8a41c9d5621f8\"," +
                "\"timestamp\":\"9999-12-31T23:59:59.9999999+00:00\"," +
                "\"logentry\":{\"message\":\"message\",\"formatted\":\"structured_message\"}," +
                "\"logger\":\"logger\"," +
                "\"platform\":\"csharp\"," +
                "\"server_name\":\"server_name\"," +
                "\"release\":\"release\"," +
                "\"exception\":{\"values\":[{\"value\":\"exception_value\"}]}," +
                "\"threads\":{\"values\":[{\"crashed\":true}]}," +
                "\"level\":\"fatal\"," +
                "\"transaction\":\"transaction\"," +
                "\"environment\":\"environment\"," +
                "\"sdk\":{\"name\":\"SDK-test\"}," +
                "\"fingerprint\":[\"fingerprint\"]," +
                "\"breadcrumbs\":[{\"timestamp\":\"9999-12-31T23:59:59Z\",\"message\":\"crumb\"}]," +
                "\"extra\":{\"extra_key\":\"extra_value\"}," +
                "\"tags\":{\"tag_key\":\"tag_value\"}}",
                actual
            );
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

        [Fact]
        public void SentryThreads_Getter_NotNull()
        {
            var evt = new SentryEvent();
            Assert.NotNull(evt.SentryThreads);
        }

        [Fact]
        public void SentryThreads_SetToNUll_Getter_NotNull()
        {
            var evt = new SentryEvent
            {
                SentryThreads = null
            };

            Assert.NotNull(evt.SentryThreads);
        }

        [Fact]
        public void SentryExceptions_Getter_NotNull()
        {
            var evt = new SentryEvent();
            Assert.NotNull(evt.SentryExceptions);
        }

        [Fact]
        public void SentryExceptions_SetToNUll_Getter_NotNull()
        {
            var evt = new SentryEvent
            {
                SentryExceptions = null
            };

            Assert.NotNull(evt.SentryExceptions);
        }

        [Fact]
        public void Modules_Getter_NotNull()
        {
            var evt = new SentryEvent();
            Assert.NotNull(evt.Modules);
        }
    }
}
