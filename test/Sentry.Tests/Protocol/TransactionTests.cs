using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using FluentAssertions;
using NSubstitute;
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
            var actual = Transaction.FromJson(Json.Parse(actualString));

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

        [Fact]
        public void StartChild_LevelOne_Works()
        {
            // Arrange
            var transaction = new Transaction(DisabledHub.Instance, "my name", "my op");

            // Act
            var child = transaction.StartChild("child op", "child desc");

            // Assert
            transaction.Spans.Should().HaveCount(1);
            transaction.Spans.Should().Contain(child);
            child.Operation.Should().Be("child op");
            child.Description.Should().Be("child desc");
            child.ParentSpanId.Should().Be(transaction.SpanId);
        }

        [Fact]
        public void StartChild_LevelTwo_Works()
        {
            // Arrange
            var transaction = new Transaction(DisabledHub.Instance, "my name", "my op");

            // Act
            var child = transaction.StartChild("child op", "child desc");
            var grandChild = child.StartChild("grandchild op", "grandchild desc");

            // Assert
            transaction.Spans.Should().HaveCount(2);
            transaction.Spans.Should().Contain(child);
            transaction.Spans.Should().Contain(grandChild);
            grandChild.Operation.Should().Be("grandchild op");
            grandChild.Description.Should().Be("grandchild desc");
            grandChild.ParentSpanId.Should().Be(child.SpanId);
        }

        [Fact]
        public void StartChild_SamplingInherited_Null()
        {
            // Arrange
            var transaction = new Transaction(DisabledHub.Instance, "my name", "my op") {IsSampled = null};

            // Act
            var child = transaction.StartChild("child op", "child desc");

            // Assert
            child.IsSampled.Should().BeNull();
        }

        [Fact]
        public void StartChild_SamplingInherited_True()
        {
            // Arrange
            var transaction = new Transaction(DisabledHub.Instance, "my name", "my op") {IsSampled = true};

            // Act
            var child = transaction.StartChild("child op", "child desc");

            // Assert
            child.IsSampled.Should().BeTrue();
        }

        [Fact]
        public void StartChild_SamplingInherited_False()
        {
            // Arrange
            var transaction = new Transaction(DisabledHub.Instance, "my name", "my op") {IsSampled = false};

            // Act
            var child = transaction.StartChild("child op", "child desc");

            // Assert
            child.IsSampled.Should().BeFalse();
        }

        [Fact]
        public void Finish_RecordsTime()
        {
            // Arrange
            var transaction = new Transaction(DisabledHub.Instance, "my name", "my op");

            // Act
            transaction.Finish();

            // Assert
            transaction.EndTimestamp.Should().NotBeNull();
            (transaction.EndTimestamp - transaction.StartTimestamp).Should().BeGreaterOrEqualTo(TimeSpan.Zero);
        }

        [Fact]
        public void Finish_CapturesTransaction()
        {
            // Arrange
            var client = Substitute.For<ISentryClient>();
            var transaction = new Transaction(client, "my name", "my op");

            // Act
            transaction.Finish();

            // Assert
            client.Received(1).CaptureTransaction(transaction);
        }
    }
}
