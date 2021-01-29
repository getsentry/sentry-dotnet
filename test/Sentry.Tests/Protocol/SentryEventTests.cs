using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Sentry.Internal;
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
                Contexts = new Contexts {
                    ["context_key"] = "context_value",
                    [".NET Framework"] = new Dictionary<string, string> {
                        [".NET Framework"] = "\"v2.0.50727\", \"v3.0\", \"v3.5\"",
                        [".NET Framework Client"] = "\"v4.8\", \"v4.0.0.0\"",
                        [".NET Framework Full"] = "\"v4.8\""
                    }
                },
                Sdk = new SdkVersion { Name = "SDK-test", Version = "1.1.1" },
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
                SentryExceptions = new[] { new SentryException { Value = "exception_value"} },
                SentryThreads = new[] { new SentryThread { Crashed = true } },
                ServerName = "server_name",
                TransactionName = "transaction",
            };

            sut.Sdk.AddPackage(new Package("name", "version"));
            sut.AddBreadcrumb(new Breadcrumb(timestamp, "crumb"));
            sut.AddBreadcrumb(new Breadcrumb(
                timestamp,
                "message",
                "type",
                new Dictionary<string, string> {{"data-key","data-value"}},
                "category",
                BreadcrumbLevel.Warning));

            sut.SetExtra("extra_key", "extra_value");
            sut.Fingerprint = new[] {"fingerprint"};
            sut.SetTag("tag_key", "tag_value");

            var actualString = sut.ToJsonString();

            var actual = SentryEvent.FromJson(Json.Parse(actualString));

            actual.Should().BeEquivalentTo(sut, o =>
            {
                // Due to timestamp precision
                o.Excluding(e => e.Breadcrumbs);
                o.Excluding(e => e.Exception);

                return o;
            });

            // Expected item[0].Timestamp to be <9999-12-31 23:59:59.9999999>, but found <9999-12-31 23:59:59.999>.
            actual.Breadcrumbs.Should().BeEquivalentTo(sut.Breadcrumbs, o => o.Excluding(b => b.Timestamp));
            var counter = 0;
            foreach (var sutBreadcrumb in sut.Breadcrumbs)
            {
                sutBreadcrumb.Timestamp.Should().BeCloseTo(actual.Breadcrumbs.ElementAt(counter++).Timestamp);
            }
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
