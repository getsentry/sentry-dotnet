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

        [Fact(Timeout = 5000)]
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
            using var transport = new FakeTransport();
            using var worker = new StatelessBackgroundWorker(transport, new SentryOptions());

            using var envelope = Envelope.FromEvent(new SentryEvent());

            transport.EnvelopeSent += (_, __) => throw new OperationCanceledException();

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
            var transport = Substitute.For<ITransport>();
            using var worker = new StatelessBackgroundWorker(transport, new SentryOptions());

            using var envelope = Envelope.FromEvent(new SentryEvent());

            transport
                .When(e => e.SendEnvelopeAsync(envelope))
                .Do(_ => throw new Exception("Sending to sentry failed."));

            // Act
            worker.EnqueueEnvelope(envelope);
            worker.Dispose();

            // Assert
            worker.WorkerTask.Status.Should().Be(TaskStatus.Faulted);
        }

        [Fact(Timeout = 5000)]
        public async Task Dispose_EventQueuedZeroShutdownTimeout_CantEmptyQueueBeforeShutdown()
        {
            // Arrange
            var transport = Substitute.For<ITransport>();

            using var worker = new StatelessBackgroundWorker(transport, new SentryOptions
            {
                ShutdownTimeout = default // Don't wait
            });

            using var envelope = Envelope.FromEvent(new SentryEvent());

            var count = 0;
            transport.SendEnvelopeAsync(Arg.Any<Envelope>()).Returns(_ =>
            {
                worker.EnqueueEnvelope(envelope);

                if (count++ > 0)
                {
                    worker.Shutdown();
                    worker.EnqueueEnvelope(envelope);
                    worker.EnqueueEnvelope(envelope);
                }

                return default;
            });

            // Act
            worker.EnqueueEnvelope(envelope);
            worker.Dispose();

            await worker.WorkerTask;

            // Assert
            // First event was sent, second hit transport with a cancelled token.
            // Third never taken from the queue
            worker.QueueLength.Should().Be(1);
        }

        [Fact(Timeout = 5000)]
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

        [Fact(Timeout = 5000)]
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

        [Fact(Timeout = 5000)]
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

        [Fact]
        public void CaptureEvent_InnerTransportThrows_WorkerSuppresses()
        {
            // Arrange
            var logger = Substitute.For<IDiagnosticLogger>();
            var transport = Substitute.For<ITransport>();

            using var worker = new StatelessBackgroundWorker(transport, new SentryOptions
            {
                DiagnosticLogger = logger,
                DiagnosticsLevel = SentryLevel.Debug
            });

            using var envelope = Envelope.FromEvent(new SentryEvent());

            transport
                .When(e => e.SendEnvelopeAsync(envelope))
                .Do(_ => throw new Exception("Sending to sentry failed."));

            // Act
            var isQueued = worker.EnqueueEnvelope(envelope);

            // Assert
            isQueued.Should().BeTrue();
            logger.Received().Log(SentryLevel.Error, "Error while processing event {1}: {0}. #{2} in queue.",
                Arg.Any<Exception>(), Arg.Any<object[]>());
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

        [Fact]
        public async Task FlushAsync_DisposedWorker_LogsAndReturns()
        {
            // Arrange
            var logger = Substitute.For<IDiagnosticLogger>();

            using var worker = new StatelessBackgroundWorker(new FakeTransport(), new SentryOptions
            {
                DiagnosticLogger = logger,
                DiagnosticsLevel = SentryLevel.Debug
            });

            using var envelope = Envelope.FromEvent(new SentryEvent());

            // Act
            worker.Dispose();
            await worker.FlushAsync(TimeSpan.MaxValue);

            // Assert
            logger.Received().Log(SentryLevel.Debug, "Worker disposed. Nothing to flush.");
        }

        [Fact]
        public async Task FlushAsync_EmptyQueue_LogsAndReturns()
        {
            // Arrange
            var logger = Substitute.For<IDiagnosticLogger>();

            using var worker = new StatelessBackgroundWorker(new FakeTransport(), new SentryOptions
            {
                DiagnosticLogger = logger,
                DiagnosticsLevel = SentryLevel.Debug
            });

            using var envelope = Envelope.FromEvent(new SentryEvent());

            // Act
            await worker.FlushAsync(TimeSpan.MaxValue);

            // Assert
            logger.Received().Log(SentryLevel.Debug, "No events to flush.");
        }

        [Fact]
        public async Task FlushAsync_SingleEvent_FlushReturnsAfterEventSent()
        {
            // Arrange
            var logger = Substitute.For<IDiagnosticLogger>();
            var transport = Substitute.For<ITransport>();
            using var worker = new StatelessBackgroundWorker(transport, new SentryOptions
            {
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

            var flushTask = worker.FlushAsync(TimeSpan.FromDays(1));

            // Assert
            worker.QueueLength.Should().Be(1);

            eventsQueuedEvent.Set();
            await flushTask;

            logger.Received().Log(SentryLevel.Debug, "Successfully flushed all events up to call to FlushAsync.");
            worker.QueueLength.Should().Be(0); // Only the item being processed at the blocked callback
        }

        [Fact]
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
            await worker.FlushAsync(TimeSpan.Zero);
        }

        [Fact]
        public async Task FlushAsync_FullQueue_RespectsTimeout()
        {
            // Arrange
            var logger = Substitute.For<IDiagnosticLogger>();
            var transport = Substitute.For<ITransport>();
            using var worker = new StatelessBackgroundWorker(transport, new SentryOptions
            {
                MaxQueueItems = 1,
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

            await worker.FlushAsync(TimeSpan.FromSeconds(1));

            // Assert
            logger.Received().Log(SentryLevel.Debug, "Timeout when trying to flush queue.");
            worker.QueueLength.Should().Be(1);
        }
    }
}
