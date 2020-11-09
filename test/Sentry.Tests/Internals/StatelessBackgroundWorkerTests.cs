using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Sentry.Extensibility;
using Sentry.Internal;
using Sentry.Protocol.Envelopes;
using Sentry.Testing;
using Xunit;

namespace Sentry.Tests.Internals
{
    public class StatelessBackgroundWorkerTests
    {
        [Fact]
        public void Ctor_Task_Created()
        {
            // Arrange & act
            using var worker = new StatelessBackgroundWorker(new FakeTransport(), new SentryOptions());

            // Assert
            worker.WorkerTask.Should().NotBeNull();
        }

        [Fact]
        public void Dispose_StopsTask()
        {
            // Arrange
            using var worker = new StatelessBackgroundWorker(new FakeTransport(), new SentryOptions());

            // Act
            worker.Dispose();

            // Assert
            worker.WorkerTask.Status.Should().Be(TaskStatus.RanToCompletion);
        }

        [Fact(Timeout = 10000)]
        public void Dispose_WhenRequestInFlight_StopsTask()
        {
            // Arrange
            var transport = Substitute.For<ITransport>();
            using var worker = new StatelessBackgroundWorker(transport, new SentryOptions());

            using var envelope = Envelope.FromEvent(new SentryEvent());

            transport.SendEnvelopeAsync(Arg.Any<Envelope>()).Returns(new ValueTask(Task.Delay(3000)));

            // Act
            worker.EnqueueEnvelope(envelope);
            worker.Dispose();

            // Assert
            worker.WorkerTask.Status.Should().Be(TaskStatus.RanToCompletion);
        }

        [Fact]
        public void Dispose_TokenCancelledWhenRequestInFlight_StopsTask()
        {
            // Arrange
            var transport = new FakeFailingTransport();
            using var worker = new StatelessBackgroundWorker(transport, new SentryOptions());

            using var envelope = Envelope.FromEvent(new SentryEvent());

            // Act
            worker.EnqueueEnvelope(envelope);
            worker.Dispose();

            // Assert
            worker.WorkerTask.Status.Should().Be(TaskStatus.RanToCompletion);
        }

        [Fact]
        public void Dispose_SwallowsException()
        {
            // Arrange
            var transport = new FakeFailingTransport();
            using var worker = new StatelessBackgroundWorker(transport, new SentryOptions());

            using var envelope = Envelope.FromEvent(new SentryEvent());

            // Act
            worker.Shutdown();
            worker.Dispose();

            // Assert
            worker.WorkerTask.Status.Should().Be(TaskStatus.RanToCompletion);
        }

        [Fact(Timeout = 10000)]
        public async Task Dispose_EventQueuedZeroShutdownTimeout_CantEmptyQueueBeforeShutdown()
        {
            // Arrange
            var transport = Substitute.For<ITransport>();

            using var worker = new StatelessBackgroundWorker(transport, new SentryOptions
            {
                ShutdownTimeout = TimeSpan.Zero // Don't wait
            });

            using var envelope = Envelope.FromEvent(new SentryEvent());

            transport.SendEnvelopeAsync(Arg.Any<Envelope>()).Returns(new ValueTask(Task.Delay(3000)));

            // Act
            worker.EnqueueEnvelope(envelope);
            worker.EnqueueEnvelope(envelope);
            worker.Dispose();

            await worker.WorkerTask;

            // Assert
            worker.QueueLength.Should().Be(1);
        }

        [Fact(Timeout = 10000)]
        public async Task Dispose_EventQueuedDefaultShutdownTimeout_EmptiesQueueBeforeShutdown()
        {
            // Arrange
            using var transport = new FakeTransport();
            using var worker = new StatelessBackgroundWorker(transport, new SentryOptions());

            using var envelope = Envelope.FromEvent(new SentryEvent());

            // Act
            worker.EnqueueEnvelope(envelope);
            worker.Shutdown();

            worker.EnqueueEnvelope(envelope);
            worker.EnqueueEnvelope(envelope);

            await worker.WorkerTask;

            worker.Dispose(); // Since token was already cancelled, it's basically blocking to wait on the task completion

            // Assert
            worker.WorkerTask.Status.Should().Be(TaskStatus.RanToCompletion);
            worker.QueueLength.Should().Be(0);
        }

        [Fact(Timeout = 10000)]
        public async Task Create_CancelledTaskAndNoShutdownTimeout_ConsumesNoEvents()
        {
            // Arrange
            using var transport = new FakeTransport();

            using var worker = new StatelessBackgroundWorker(transport, new SentryOptions
            {
                ShutdownTimeout = default // Don't wait
            });

            using var envelope = Envelope.FromEvent(new SentryEvent());

            // Act
            worker.Shutdown();
            await worker.WorkerTask;

            // Assert
            worker.WorkerTask.Status.Should().Be(TaskStatus.RanToCompletion);
            worker.Dispose(); // no-op as task is already finished
        }

        [Fact(Timeout = 10000)]
        public void CaptureEvent_LimitReached_EventDropped()
        {
            // Arrange
            var transport = Substitute.For<ITransport>();

            using var worker = new StatelessBackgroundWorker(transport, new SentryOptions
            {
                MaxQueueItems = 1
            });

            using var envelope = Envelope.FromEvent(new SentryEvent());

            transport.SendEnvelopeAsync(Arg.Any<Envelope>()).Returns(new ValueTask(Task.Delay(3000)));

            // Act
            worker.EnqueueEnvelope(envelope);
            var isSecondQueued = worker.EnqueueEnvelope(envelope);

            // Assert
            isSecondQueued.Should().BeFalse();
            worker.QueueLength.Should().BeLessOrEqualTo(1);
        }

        [Fact]
        public void CaptureEvent_DisposedWorker_ThrowsObjectDisposedException()
        {
            // Arrange
            using var worker = new StatelessBackgroundWorker(new FakeTransport(), new SentryOptions());
            using var envelope = Envelope.FromEvent(new SentryEvent());

            worker.Dispose();

            // Act & assert
            Assert.Throws<ObjectDisposedException>(() => worker.EnqueueEnvelope(envelope));
        }

        [Fact]
        public void CaptureEvent_InnerTransportInvoked()
        {
            // Arrange
            using var transport = new FakeTransport();
            using var worker = new StatelessBackgroundWorker(transport, new SentryOptions());

            using var envelope = Envelope.FromEvent(new SentryEvent());

            // Act
            var isQueued = worker.EnqueueEnvelope(envelope);
            worker.Dispose();

            // Assert
            isQueued.Should().BeTrue();
            transport.GetSentEnvelopes().Should().HaveCount(1);
        }

        [Fact(Timeout = 10000)]
        public async Task CaptureEvent_InnerTransportThrows_WorkerSuppresses()
        {
            // Arrange
            var logger = new AccumulativeDiagnosticLogger();
            var transport = new FakeFailingTransport();

            using var worker = new StatelessBackgroundWorker(transport, new SentryOptions
            {
                Debug = true,
                DiagnosticLogger = logger
            });

            using var envelope = Envelope.FromEvent(new SentryEvent());

            // Act
            var isQueued = worker.EnqueueEnvelope(envelope);
            await worker.FlushAsync(TimeSpan.FromSeconds(3));

            // Assert
            isQueued.Should().BeTrue();
            logger.Entries.Should().Contain(e =>
                e.Level == SentryLevel.Error &&
                e.Message == "Error while processing event {0}. #{1} in queue."
            );
        }

        [Fact]
        public void QueuedItems_StartsEmpty()
        {
            // Arrange
            using var worker = new StatelessBackgroundWorker(new FakeTransport(), new SentryOptions());

            // Assert
            worker.QueueLength.Should().Be(0);
        }

        [Fact]
        public void QueuedItems_ReflectsQueue()
        {
            // Arrange
            using var worker = new StatelessBackgroundWorker(new FakeTransport(), new SentryOptions());

            using var envelope = Envelope.FromEvent(new SentryEvent());

            // Act
            worker.EnqueueEnvelope(envelope);

            // Assert
            worker.QueueLength.Should().Be(1);
        }

        [Fact(Timeout = 10000)]
        public async Task FlushAsync_DisposedWorker_LogsAndReturns()
        {
            // Arrange
            var logger = new AccumulativeDiagnosticLogger();

            using var worker = new StatelessBackgroundWorker(new FakeTransport(), new SentryOptions
            {
                Debug = true,
                DiagnosticLogger = logger
            });

            using var envelope = Envelope.FromEvent(new SentryEvent());

            // Act
            worker.Dispose();
            await worker.FlushAsync(TimeSpan.FromSeconds(3));

            // Assert
            logger.Entries.Should().Contain(e =>
                e.Level == SentryLevel.Debug &&
                e.Message == "Worker disposed. Nothing to flush."
            );
        }

        [Fact(Timeout = 10000)]
        public async Task FlushAsync_EmptyQueue_LogsAndReturns()
        {
            // Arrange
            var logger = new AccumulativeDiagnosticLogger();

            using var worker = new StatelessBackgroundWorker(new FakeTransport(), new SentryOptions
            {
                Debug = true,
                DiagnosticLogger = logger
            });

            using var envelope = Envelope.FromEvent(new SentryEvent());

            // Act
            await worker.FlushAsync(TimeSpan.FromSeconds(3));

            // Assert
            logger.Entries.Should().Contain(e =>
                e.Level == SentryLevel.Debug &&
                e.Message == "No events to flush."
            );
        }

        [Fact(Timeout = 10000)]
        public async Task FlushAsync_SingleEvent_FlushReturnsAfterEventSent()
        {
            // Arrange
            var logger = new AccumulativeDiagnosticLogger();
            var transport = Substitute.For<ITransport>();
            using var worker = new StatelessBackgroundWorker(transport, new SentryOptions
            {
                Debug = true,
                DiagnosticLogger = logger
            });

            using var envelope = Envelope.FromEvent(new SentryEvent());

            using var transportEvent = new ManualResetEventSlim(false);
            using var eventsQueuedEvent = new ManualResetEventSlim(false);

            transport
                .When(t => t.SendEnvelopeAsync(envelope, Arg.Any<CancellationToken>()))
                .Do(p =>
                {
                    transportEvent.Set(); // Processing first event
                    eventsQueuedEvent.Wait(); // Stay blocked while test queue events
                });

            // Act
            worker.EnqueueEnvelope(envelope);
            transportEvent.Wait(); // Wait first event to be in-flight

            var flushTask = worker.FlushAsync(TimeSpan.FromSeconds(3));

            // Assert
            worker.QueueLength.Should().Be(1);

            eventsQueuedEvent.Set();
            await flushTask;

            logger.Entries.Should().Contain(e =>
                e.Level == SentryLevel.Debug &&
                e.Message == "Successfully flushed all events up to call to FlushAsync."
            );

            worker.QueueLength.Should().Be(0); // Only the item being processed at the blocked callback
        }

        [Fact(Timeout = 10000)]
        public async Task FlushAsync_ZeroTimeout_Accepted()
        {
            // Arrange
            var transport = Substitute.For<ITransport>();
            using var worker = new StatelessBackgroundWorker(transport, new SentryOptions());

            using var envelope = Envelope.FromEvent(new SentryEvent());

            using var transportEvent = new ManualResetEventSlim(false);
            using var eventsQueuedEvent = new ManualResetEventSlim(false);

            transport
                .When(t => t.SendEnvelopeAsync(envelope, Arg.Any<CancellationToken>()))
                .Do(p =>
                {
                    transportEvent.Set(); // Processing first event
                    eventsQueuedEvent.Wait(); // Stay blocked while test queue events
                });

            // Act
            worker.EnqueueEnvelope(envelope);
            transportEvent.Wait(); // Wait first event to be in-flight
            await worker.FlushAsync(TimeSpan.FromSeconds(3));
        }

        [Fact(Timeout = 10000)]
        public async Task FlushAsync_FullQueue_RespectsTimeout()
        {
            // Arrange
            var logger = new AccumulativeDiagnosticLogger();
            var transport = Substitute.For<ITransport>();
            using var worker = new StatelessBackgroundWorker(transport, new SentryOptions
            {
                MaxQueueItems = 1,
                Debug = true,
                DiagnosticLogger = logger
            });

            using var envelope = Envelope.FromEvent(new SentryEvent());

            using var transportEvent = new ManualResetEventSlim(false);
            using var eventsQueuedEvent = new ManualResetEventSlim(false);

            transport
                .When(t => t.SendEnvelopeAsync(envelope, Arg.Any<CancellationToken>()))
                .Do(p =>
                {
                    transportEvent.Set(); // Processing first event
                    eventsQueuedEvent.Wait(); // Stay blocked while test queue events
                });

            // Act
            worker.EnqueueEnvelope(envelope);
            transportEvent.Wait(); // Wait first event to be in-flight

            await worker.FlushAsync(TimeSpan.FromSeconds(3));

            // Assert
            logger.Entries.Should().Contain(e =>
                e.Level == SentryLevel.Debug &&
                e.Message == "Timeout when trying to flush queue."
            );

            worker.QueueLength.Should().Be(1);
        }
    }
}
