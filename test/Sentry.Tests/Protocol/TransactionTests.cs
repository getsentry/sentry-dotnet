using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Sentry.Extensibility;
using Sentry.Internal;
using Sentry.Protocol;
using Xunit;

namespace Sentry.Tests.Protocol
{
    public class TransactionTests
    {
        [Fact]
        public void SerializeObject_AllPropertiesSetToNonDefault_SerializesValidObject()
        {
            // Arrange
            var timestamp = DateTimeOffset.MaxValue;
            var transaction = new Transaction(DisabledHub.Instance, "name123", "op123")
            {
                Description = "desc123",
                Status = SpanStatus.Aborted,
                User = new User { Id = "user-id" },
                Request = new Request { Method = "POST" },
                Sdk = new SdkVersion { Name = "SDK-test", Version = "1.1.1" },
                Environment = "environment",
                Level = SentryLevel.Fatal,
            };

            // Don't overwrite the contexts object as it contains trace data.
            // See https://github.com/getsentry/sentry-dotnet/issues/752
            transaction.Contexts["context_key"] = "context_value";
            transaction.Contexts[".NET Framework"] = new Dictionary<string, string>
            {
                [".NET Framework"] = "\"v2.0.50727\", \"v3.0\", \"v3.5\"",
                [".NET Framework Client"] = "\"v4.8\", \"v4.0.0.0\"",
                [".NET Framework Full"] = "\"v4.8\""
            };

            transaction.Sdk.AddPackage(new Package("name", "version"));
            transaction.AddBreadcrumb(new Breadcrumb(timestamp, "crumb"));
            transaction.AddBreadcrumb(new Breadcrumb(
                timestamp,
                "message",
                "type",
                new Dictionary<string, string> {{"data-key", "data-value"}},
                "category",
                BreadcrumbLevel.Warning)
            );

            transaction.SetExtra("extra_key", "extra_value");
            transaction.Fingerprint = new[] {"fingerprint"};
            transaction.SetTag("tag_key", "tag_value");

            var child1 = transaction.StartChild("child_op123", "child_desc123");
            child1.Status = SpanStatus.Unimplemented;
            child1.SetTag("q", "v");
            child1.SetExtra("f", "p");
            child1.Finish(SpanStatus.Unimplemented);

            var child2 = transaction.StartChild("child_op999", "child_desc999");
            child2.Status = SpanStatus.OutOfRange;
            child2.SetTag("xxx", "zzz");
            child2.SetExtra("f222", "p111");
            child2.Finish(SpanStatus.OutOfRange);

            transaction.Finish(SpanStatus.Aborted);

            // Act
            var actualString = transaction.ToJsonString();
            var actual = SentryEvent.FromJson(Json.Parse(actualString));

            // Assert
            actual.Should().BeEquivalentTo(transaction, o =>
            {
                // Due to timestamp precision
                o.Excluding(e => e.Breadcrumbs);

                return o;
            });

            // Expected item[0].Timestamp to be <9999-12-31 23:59:59.9999999>, but found <9999-12-31 23:59:59.999>.
            actual.Breadcrumbs.Should().BeEquivalentTo(transaction.Breadcrumbs, o => o.Excluding(b => b.Timestamp));
            var counter = 0;
            foreach (var sutBreadcrumb in transaction.Breadcrumbs)
            {
                sutBreadcrumb.Timestamp.Should().BeCloseTo(actual.Breadcrumbs.ElementAt(counter++).Timestamp);
            }
        }
    }
}
