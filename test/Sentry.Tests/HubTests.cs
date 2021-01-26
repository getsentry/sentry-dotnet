using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Sentry;
using Sentry.Extensibility;
using Sentry.Internal;
using Sentry.Protocol;
using Sentry.Protocol.Envelopes;
using Xunit;

// ReSharper disable once CheckNamespace
// Tests code path which excludes frames with namespace Sentry
namespace NotSentry.Tests
{
    public class HubTests
    {
        private class FakeBackgroundWorker : IBackgroundWorker
        {
            public List<Envelope> Queue { get; } = new();

            public int QueuedItems => Queue.Count;

            public bool EnqueueEnvelope(Envelope envelope)
            {
                Queue.Add(envelope);
                return true;
            }

            public Task FlushAsync(TimeSpan timeout) => Task.CompletedTask;
        }

        [Fact]
        public void PushScope_BreadcrumbWithinScope_NotVisibleOutside()
        {
            // Arrange
            var hub = new Hub(new SentryOptions
            {
                Dsn = DsnSamples.ValidDsnWithSecret,
                BackgroundWorker = new FakeBackgroundWorker()
            });

            // Act & assert
            using (hub.PushScope())
            {
                hub.ConfigureScope(s => s.AddBreadcrumb("test"));
                Assert.Single(hub.ScopeManager.GetCurrent().Key.Breadcrumbs);
            }

            Assert.Empty(hub.ScopeManager.GetCurrent().Key.Breadcrumbs);
        }

        [Fact]
        public void PushAndLockScope_DoesNotAffectOuterScope()
        {
            // Arrange
            var hub = new Hub(new SentryOptions
            {
                Dsn = DsnSamples.ValidDsnWithSecret,
                BackgroundWorker = new FakeBackgroundWorker()
            });

            // Act & assert
            hub.ConfigureScope(s => Assert.False(s.Locked));
            using (hub.PushAndLockScope())
            {
                hub.ConfigureScope(s => Assert.True(s.Locked));
            }

            hub.ConfigureScope(s => Assert.False(s.Locked));
        }

        [Fact]
        public void CaptureMessage_AttachStacktraceFalse_DoesNotIncludeStackTrace()
        {
            // Arrange
            var worker = new FakeBackgroundWorker();

            var hub = new Hub(new SentryOptions
            {
                Dsn = DsnSamples.ValidDsnWithSecret,
                BackgroundWorker = worker,
                AttachStacktrace = true
            });

            // Act
            hub.CaptureMessage("test");

            // Assert
            var envelope = worker.Queue.Single();

            var stackTrace = envelope.Items
                .Select(i => i.Payload)
                .OfType<JsonSerializable>()
                .Select(i => i.Source)
                .OfType<SentryEvent>()
                .Single()
                .SentryExceptionValues;

            stackTrace.Should().BeNull();
        }

        [Fact]
        public void CaptureMessage_FailedQueue_LastEventIdSetToEmpty()
        {
            // Arrange
            var worker = Substitute.For<IBackgroundWorker>();
            worker.EnqueueEnvelope(Arg.Any<Envelope>()).Returns(false);

            var hub = new Hub(new SentryOptions
            {
                Dsn = DsnSamples.ValidDsnWithSecret,
                BackgroundWorker = worker
            });

            // Act
            var actualId = hub.CaptureMessage("test");

            // Assert
            Assert.Equal(Guid.Empty, (Guid)actualId);
            Assert.Equal(Guid.Empty, (Guid)hub.LastEventId);
        }

        [Fact]
        public void CaptureMessage_SuccessQueued_LastEventIdSetToReturnedId()
        {
            // Arrange
            var worker = Substitute.For<IBackgroundWorker>();
            worker.EnqueueEnvelope(Arg.Any<Envelope>()).Returns(true);

            var hub = new Hub(new SentryOptions
            {
                Dsn = DsnSamples.ValidDsnWithSecret,
                BackgroundWorker = worker
            });

            // Act
            var actualId = hub.CaptureMessage("test");

            // Assert
            Assert.NotEqual(default, actualId);
            Assert.Equal(actualId, hub.LastEventId);
        }

        [Fact]
        public void StartTransaction_NameOpDescription_Works()
        {
            // Arrange
            var hub = new Hub(new SentryOptions
            {
                Dsn = DsnSamples.ValidDsnWithSecret
            });

            // Act
            var transaction = hub.StartTransaction("name", "operation", "description");

            // Assert
            transaction.Name.Should().Be("name");
            transaction.Operation.Should().Be("operation");
            transaction.Description.Should().Be("description");
        }

        [Fact]
        public void StartTransaction_FromTraceHeader_Works()
        {
            // Arrange
            var hub = new Hub(new SentryOptions
            {
                Dsn = DsnSamples.ValidDsnWithSecret
            });

            var traceHeader = new SentryTraceHeader(
                SentryId.Parse("75302ac48a024bde9a3b3734a82e36c8"),
                SpanId.Parse("2000000000000000"),
                true
            );

            // Act
            var transaction = hub.StartTransaction("name", "operation", traceHeader);

            // Assert
            transaction.TraceId.Should().Be(SentryId.Parse("75302ac48a024bde9a3b3734a82e36c8"));
            transaction.ParentSpanId.Should().Be(SpanId.Parse("2000000000000000"));
            transaction.IsSampled.Should().BeTrue();
        }

        [Fact]
        public void StartTransaction_StaticSampling_SampledIn()
        {
            // Arrange
            var hub = new Hub(new SentryOptions
            {
                Dsn = DsnSamples.ValidDsnWithSecret,
                TracesSampleRate = 1
            });

            // Act
            var transaction = hub.StartTransaction("name", "operation");

            // Assert
            transaction.IsSampled.Should().BeTrue();
        }

        [Fact]
        public void StartTransaction_StaticSampling_SampledOut()
        {
            // Arrange
            var hub = new Hub(new SentryOptions
            {
                Dsn = DsnSamples.ValidDsnWithSecret,
                TracesSampleRate = 0
            });

            // Act
            var transaction = hub.StartTransaction("name", "operation");

            // Assert
            transaction.IsSampled.Should().BeFalse();
        }

        [Fact]
        public void StartTransaction_DynamicSampling_SampledIn()
        {
            // Arrange
            var hub = new Hub(new SentryOptions
            {
                Dsn = DsnSamples.ValidDsnWithSecret,
                TracesSampler = ctx => ctx.TransactionContext.Name == "foo" ? 1 : 0
            });

            // Act
            var transaction = hub.StartTransaction("foo", "op");

            // Assert
            transaction.IsSampled.Should().BeTrue();
        }

        [Fact]
        public void StartTransaction_DynamicSampling_SampledOut()
        {
            // Arrange
            var hub = new Hub(new SentryOptions
            {
                Dsn = DsnSamples.ValidDsnWithSecret,
                TracesSampler = ctx => ctx.TransactionContext.Name == "foo" ? 1 : 0
            });

            // Act
            var transaction = hub.StartTransaction("bar", "op");

            // Assert
            transaction.IsSampled.Should().BeFalse();
        }

        [Fact]
        public void StartTransaction_DynamicSampling_WithCustomContext_SampledIn()
        {
            // Arrange
            var hub = new Hub(new SentryOptions
            {
                Dsn = DsnSamples.ValidDsnWithSecret,
                TracesSampler = ctx => ctx.CustomSamplingContext.GetValueOrDefault("xxx") as string == "zzz" ? 1 : 0
            });

            // Act
            var transaction = hub.StartTransaction(
                new TransactionContext("foo", "op"),
                new Dictionary<string, object> {["xxx"] = "zzz"}
            );

            // Assert
            transaction.IsSampled.Should().BeTrue();
        }

        [Fact]
        public void StartTransaction_DynamicSampling_WithCustomContext_SampledOut()
        {
            // Arrange
            var hub = new Hub(new SentryOptions
            {
                Dsn = DsnSamples.ValidDsnWithSecret,
                TracesSampler = ctx => ctx.CustomSamplingContext.GetValueOrDefault("xxx") as string == "zzz" ? 1 : 0
            });

            // Act
            var transaction = hub.StartTransaction(
                new TransactionContext("foo", "op"),
                new Dictionary<string, object> {["xxx"] = "yyy"}
            );

            // Assert
            transaction.IsSampled.Should().BeFalse();
        }

        [Fact]
        public void StartTransaction_DynamicSampling_FallbackToStatic_SampledIn()
        {
            // Arrange
            var hub = new Hub(new SentryOptions
            {
                Dsn = DsnSamples.ValidDsnWithSecret,
                TracesSampler = _ => null,
                TracesSampleRate = 1
            });

            // Act
            var transaction = hub.StartTransaction("foo", "bar");

            // Assert
            transaction.IsSampled.Should().BeTrue();
        }

        [Fact]
        public void StartTransaction_DynamicSampling_FallbackToStatic_SampledOut()
        {
            // Arrange
            var hub = new Hub(new SentryOptions
            {
                Dsn = DsnSamples.ValidDsnWithSecret,
                TracesSampler = _ => null,
                TracesSampleRate = 0
            });

            // Act
            var transaction = hub.StartTransaction("foo", "bar");

            // Assert
            transaction.IsSampled.Should().BeFalse();
        }

        [Fact]
        public void StartTransaction_ContainsSdk()
        {
            // Arrange
            var hub = new Hub(new SentryOptions
            {
                Dsn = DsnSamples.ValidDsnWithSecret
            });

            // Act
            var transaction = hub.StartTransaction("name", "operation");

            // Assert
            transaction.Sdk.Name.Should().NotBeNullOrWhiteSpace();
            transaction.Sdk.Version.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public void StartTransaction_ContainsRelease()
        {
            // Arrange
            var hub = new Hub(new SentryOptions
            {
                Dsn = DsnSamples.ValidDsnWithSecret
            });

            // Act
            var transaction = hub.StartTransaction("name", "operation");

            // Assert
            transaction.Release.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public void StartTransaction_ContainsEnvironment()
        {
            // Arrange
            var hub = new Hub(new SentryOptions
            {
                Dsn = DsnSamples.ValidDsnWithSecret
            });

            // Act
            var transaction = hub.StartTransaction("name", "operation");

            // Assert
            transaction.Environment.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public void StartTransaction_ContainsTagsFromScope()
        {
            // Arrange
            var hub = new Hub(new SentryOptions
            {
                Dsn = DsnSamples.ValidDsnWithSecret
            });

            hub.ConfigureScope(scope =>
            {
                scope.SetTag("foo", "bar");

                // Act
                var transaction = hub.StartTransaction("name", "operation");

                // Assert
                transaction.Tags.Should().Contain(tag => tag.Key == "foo" && tag.Value == "bar");
            });
        }

        [Fact]
        public void GetTraceHeader_ReturnsHeaderForActiveSpan()
        {
            // Arrange
            var hub = new Hub(new SentryOptions
            {
                Dsn = DsnSamples.ValidDsnWithSecret
            });

            var transaction = hub.StartTransaction("foo", "bar");

            // Act
            hub.WithScope(scope =>
            {
                scope.Transaction = transaction;

                var header = hub.GetTraceHeader();

                // Assert
                header.Should().NotBeNull();
                header?.SpanId.Should().Be(transaction.SpanId);
                header?.TraceId.Should().Be(transaction.TraceId);
                header?.IsSampled.Should().Be(transaction.IsSampled);
            });
        }

        [Fact]
        public void CaptureTransaction_AfterTransactionFinishes_ResetsTransactionOnScope()
        {
            // Arrange
            var client = Substitute.For<ISentryClient>();

            var hub = new Hub(client, new SentryOptions
            {
                Dsn = DsnSamples.ValidDsnWithSecret
            });

            var transaction = hub.StartTransaction("foo", "bar");

            hub.WithScope(scope => scope.Transaction = transaction);

            // Act
            transaction.Finish();

            // Assert
            hub.WithScope(scope => scope.Transaction.Should().BeNull());
        }
    }
}
