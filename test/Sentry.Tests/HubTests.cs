using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Sentry;
using Sentry.Extensibility;
using Sentry.Internal;
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
            public List<Envelope> Queue { get; } = new List<Envelope>();

            public int QueuedItems => Queue.Count;

            public bool EnqueueEnvelope(Envelope envelope)
            {
                Queue.Add(envelope);
                return true;
            }

            public Task FlushAsync(TimeSpan timeout) => default;
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
    }
}
