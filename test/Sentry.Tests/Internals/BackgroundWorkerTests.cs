using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Sentry.Extensibility;
using Sentry.Internal;
using Sentry.Protocol.Envelopes;
using Xunit;

namespace Sentry.Tests.Internals
{
    public class BackgroundWorkerTests
    {
        private class Fixture
        {
            public ITransport Transport { get; set; } = Substitute.For<ITransport>();
            public IDiagnosticLogger Logger { get; set; } = Substitute.For<IDiagnosticLogger>();
            public ConcurrentQueue<Envelope> Queue { get; set; } = new();
            public CancellationTokenSource CancellationTokenSource { get; set; } = new();
            public SentryOptions SentryOptions { get; set; } = new();

            public Fixture()
            {
                _ = Logger.IsEnabled(Arg.Any<SentryLevel>()).Returns(true);
                SentryOptions.Debug = true;
                SentryOptions.DiagnosticLogger = Logger;
            }

            public BackgroundWorker GetSut()
                => new(
                    Transport,
                    SentryOptions,
                    CancellationTokenSource,
                    Queue);
        }

        private readonly Fixture _fixture = new();

        [Fact]
        public void Ctor_Task_Created()
        {
            using var sut = _fixture.GetSut();
            Assert.NotNull(sut.WorkerTask);
        }

        [Fact]
        public void Dispose_StopsTask()
        {
            var sut = _fixture.GetSut();
            sut.Dispose();

            Assert.Equal(TaskStatus.RanToCompletion, sut.WorkerTask.Status);
        }

        [Fact]
        public void Dispose_WhenRequestInFlight_StopsTask()
        {
            var signal = new ManualResetEventSlim();
            var envelope = Envelope.FromEvent(new SentryEvent());

            _fixture.Transport
                .When(t => t.SendEnvelopeAsync(envelope, Arg.Any<CancellationToken>()))
                .Do(_ => signal.Set());

            var sut = _fixture.GetSut();
            _ = sut.EnqueueEnvelope(envelope);

            Assert.True(signal.Wait(TimeSpan.FromSeconds(3)));

            sut.Dispose();

            Assert.Equal(TaskStatus.RanToCompletion, sut.WorkerTask.Status);
        }

        [Fact]
        public void Dispose_TokenCancelledWhenRequestInFlight_StopsTask()
        {
            var envelope = Envelope.FromEvent(new SentryEvent());

            _ = _fixture.Transport
                    .SendEnvelopeAsync(envelope, Arg.Any<CancellationToken>())
                    .Throws(new OperationCanceledException());

            var sut = _fixture.GetSut();
            _ = sut.EnqueueEnvelope(envelope);

            sut.Dispose();

            Assert.Equal(TaskStatus.RanToCompletion, sut.WorkerTask.Status);
        }

        [Fact]
        public void Dispose_SwallowsException()
        {
            _fixture.CancellationTokenSource.Dispose();
            var sut = _fixture.GetSut();

            _ = Assert.Throws<AggregateException>(() => sut.WorkerTask.Wait(TimeSpan.FromSeconds(3)));
            sut.Dispose();

            Assert.Equal(TaskStatus.Faulted, sut.WorkerTask.Status);
        }

        [Fact]
        public void Dispose_EventQueuedZeroShutdownTimeout_CantEmptyQueueBeforeShutdown()
        {
            _fixture.SentryOptions.ShutdownTimeout = default; // Don't wait

            var envelope = Envelope.FromEvent(new SentryEvent());

            using var sut = _fixture.GetSut();

            _fixture.Transport
                .When(t => t.SendEnvelopeAsync(envelope, Arg.Any<CancellationToken>()))
                .Do(p =>
                {
                    var token = p.ArgAt<CancellationToken>(1);
                    token.ThrowIfCancellationRequested();

                    _ = sut.EnqueueEnvelope(envelope);

                    sut.Dispose(); // Make sure next round awaits with a cancelled token
                });

            _ = sut.EnqueueEnvelope(envelope);

            Assert.True(sut.WorkerTask.Wait(TimeSpan.FromSeconds(5)));

            // First event was sent, second hit transport with a cancelled token.
            // Third never taken from the queue
            _ = Assert.Single(_fixture.Queue);
        }

        [Fact]
        public void Dispose_EventQueuedDefaultShutdownTimeout_EmptiesQueueBeforeShutdown()
        {
            var sync = new AutoResetEvent(false);

            var envelope = Envelope.FromEvent(new SentryEvent());

            using var sut = _fixture.GetSut();

            var counter = 0;
            _fixture.Transport
                .When(t => t.SendEnvelopeAsync(envelope, Arg.Any<CancellationToken>()))
                .Do(p =>
                {
                    if (++counter == 2)
                    {
                        var token = p.ArgAt<CancellationToken>(1);
                        Assert.True(token.IsCancellationRequested);
                        throw new OperationCanceledException();
                    }

                    _ = sut.EnqueueEnvelope(envelope);
                    _ = sut.EnqueueEnvelope(envelope);

                    _ = sync.Set();
                    _ = sync.WaitOne();
                });

            _ = sut.EnqueueEnvelope(envelope);

            Assert.True(sync.WaitOne(TimeSpan.FromSeconds(2)));
            _fixture.CancellationTokenSource.Cancel(); // Make sure next round awaits with a cancelled token
            _ = sync.Set();
            sut.Dispose(); // Since token was already cancelled, it's basically blocking to wait on the task completion

            Assert.Equal(TaskStatus.RanToCompletion, sut.WorkerTask.Status);
            Assert.Empty(_fixture.Queue);
        }

        [Fact]
        public void Create_CancelledTaskAndNoShutdownTimeout_ConsumesNoEvents()
        {
            // Arrange
            _fixture.SentryOptions.ShutdownTimeout = default;
            _fixture.CancellationTokenSource.Cancel();

            // Act
            using var sut = _fixture.GetSut();

            // Make sure task has finished
            Assert.True(sut.WorkerTask.Wait(TimeSpan.FromSeconds(3)));

            // Assert
            Assert.Equal(TaskStatus.RanToCompletion, sut.WorkerTask.Status);
            sut.Dispose(); // no-op as task is already finished
        }

        [Fact]
        public void CaptureEvent_LimitReached_EventDropped()
        {
            // Arrange
            var envelope = Envelope.FromEvent(new SentryEvent());

            var transportEvent = new ManualResetEvent(false);
            var eventsQueuedEvent = new ManualResetEvent(false);

            _fixture.SentryOptions.MaxQueueItems = 1;
            _fixture.Transport
                .When(t => t.SendEnvelopeAsync(envelope, Arg.Any<CancellationToken>()))
                .Do(p =>
                {
                    _ = transportEvent.Set(); // Processing first event
                    _ = eventsQueuedEvent.WaitOne(); // Stay blocked while test queue events
                });

            using var sut = _fixture.GetSut();

            // Act
            _ = sut.EnqueueEnvelope(envelope);
            _ = transportEvent.WaitOne(); // Wait first event to be in-flight

            // in-flight events are kept in queue until completed.
            var queued = sut.EnqueueEnvelope(envelope);
            Assert.False(queued); // Fails to queue second

            _ = eventsQueuedEvent.Set();
        }

        [Fact]
        public void CaptureEvent_DisposedWorker_ThrowsObjectDisposedException()
        {
            // Arrange
            var envelope = Envelope.FromEvent(new SentryEvent());

            using var sut = _fixture.GetSut();
            sut.Dispose();

            _ = Assert.Throws<ObjectDisposedException>(() => sut.EnqueueEnvelope(envelope));
        }

        [Fact]
        public void CaptureEvent_InnerTransportInvoked()
        {
            // Arrange
            var envelope = Envelope.FromEvent(new SentryEvent());

            var sut = _fixture.GetSut();

            // Act
            var queued = sut.EnqueueEnvelope(envelope);
            sut.Dispose();

            // Assert
            Assert.True(queued);
            _ = _fixture.Transport.Received(1).SendEnvelopeAsync(envelope, Arg.Any<CancellationToken>());
        }

        [Fact]
        public void CaptureEvent_InnerTransportThrows_WorkerSuppresses()
        {
            // Arrange
            var envelope = Envelope.FromEvent(new SentryEvent());

            _fixture.Transport
                .When(e => e.SendEnvelopeAsync(envelope))
                .Do(_ => throw new Exception("Sending to sentry failed."));

            using var sut = _fixture.GetSut();
            // Act
            var queued = sut.EnqueueEnvelope(envelope);

            // Assert
            Assert.True(queued);

            // TODO: tap into exception handling (some ILogger or Action<Exception>)
        }

        [Fact]
        public void QueuedItems_StartsEmpty()
        {
            using var sut = _fixture.GetSut();
            Assert.Equal(0, sut.QueuedItems);
        }

        [Fact]
        public void QueuedItems_ReflectsQueue()
        {
            _fixture.Queue.Enqueue(null);
            using var sut = _fixture.GetSut();
            Assert.Equal(1, sut.QueuedItems);
        }

        [Fact]
        public async Task FlushAsync_DisposedWorker_LogsAndReturns()
        {
            var sut = _fixture.GetSut();
            sut.Dispose();
            await sut.FlushAsync(TimeSpan.MaxValue);
            _fixture.Logger.Received().Log(SentryLevel.Debug, "Worker disposed. Nothing to flush.");
        }

        [Fact]
        public async Task FlushAsync_EmptyQueue_LogsAndReturns()
        {
            var sut = _fixture.GetSut();
            await sut.FlushAsync(TimeSpan.MaxValue);
            _fixture.Logger.Received().Log(SentryLevel.Debug, "No events to flush.");
        }

        [Fact]
        public async Task FlushAsync_SingleEvent_FlushReturnsAfterEventSent()
        {
            // Arrange
            var envelope = Envelope.FromEvent(new SentryEvent());

            var transportEvent = new ManualResetEvent(false);
            var eventsQueuedEvent = new ManualResetEvent(false);

            _fixture.Transport
                .When(t => t.SendEnvelopeAsync(envelope, Arg.Any<CancellationToken>()))
                .Do(p =>
                {
                    _ = transportEvent.Set(); // Processing first event
                    _ = eventsQueuedEvent.WaitOne(); // Stay blocked while test queue events
                });

            using var sut = _fixture.GetSut();

            // Act
            _ = sut.EnqueueEnvelope(envelope);
            _ = transportEvent.WaitOne(); // Wait first event to be in-flight

            var flushTask = sut.FlushAsync(TimeSpan.FromDays(1));
            _ = Assert.Single(_fixture.Queue); // Event being processed

            _ = eventsQueuedEvent.Set();
            await flushTask;

            _fixture.Logger.Received().Log(SentryLevel.Debug, "Successfully flushed all events up to call to FlushAsync.");
            Assert.Empty(_fixture.Queue); // Only the item being processed at the blocked callback
        }

        [Fact]
        public async Task FlushAsync_ZeroTimeout_Accepted()
        {
            // Arrange
            var envelope = Envelope.FromEvent(new SentryEvent());

            var transportEvent = new ManualResetEvent(false);
            var eventsQueuedEvent = new ManualResetEvent(false);

            _fixture.Transport
                .When(t => t.SendEnvelopeAsync(envelope, Arg.Any<CancellationToken>()))
                .Do(p =>
                {
                    _ = transportEvent.Set(); // Processing first event
                    _ = eventsQueuedEvent.WaitOne(); // Stay blocked while test queue events
                });

            using var sut = _fixture.GetSut();

            // Act
            _ = sut.EnqueueEnvelope(envelope);
            _ = transportEvent.WaitOne(); // Wait first event to be in-flight

            await sut.FlushAsync(TimeSpan.Zero);
        }

        [Fact]
        public async Task FlushAsync_FullQueue_RespectsTimeout()
        {
            // Arrange
            var envelope = Envelope.FromEvent(new SentryEvent());

            var transportEvent = new ManualResetEvent(false);
            var eventsQueuedEvent = new ManualResetEvent(false);

            _fixture.SentryOptions.MaxQueueItems = 1;
            _fixture.Transport
                .When(t => t.SendEnvelopeAsync(envelope, Arg.Any<CancellationToken>()))
                .Do(p =>
                {
                    _ = transportEvent.Set(); // Processing first event
                    _ = eventsQueuedEvent.WaitOne(); // Stay blocked while test queue events
                });

            using var sut = _fixture.GetSut();

            // Act
            _ = sut.EnqueueEnvelope(envelope);
            _ = transportEvent.WaitOne(); // Wait first event to be in-flight

            await sut.FlushAsync(TimeSpan.FromSeconds(1));

            _fixture.Logger.Received().Log(SentryLevel.Debug, "Timeout when trying to flush queue.");
            _ = Assert.Single(_fixture.Queue); // Only the item being processed at the blocked callback
        }
    }
}
