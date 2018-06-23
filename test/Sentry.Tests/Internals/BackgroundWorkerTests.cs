using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Sentry.Extensibility;
using Sentry.Internal;
using Xunit;

namespace Sentry.Tests.Internals
{
    public class BackgroundWorkerTests
    {
        private class Fixture
        {
            public ITransport Transport { get; set; } = Substitute.For<ITransport>();
            public IProducerConsumerCollection<SentryEvent> Queue { get; set; } = new ConcurrentQueue<SentryEvent>();
            public CancellationTokenSource CancellationTokenSource { get; set; } = new CancellationTokenSource();
            public BackgroundWorkerOptions BackgroundWorkerOptions { get; set; } = new BackgroundWorkerOptions();

            public BackgroundWorker GetSut()
                => new BackgroundWorker(
                    Transport,
                    BackgroundWorkerOptions,
                    CancellationTokenSource,
                    Queue);
        }

        private readonly Fixture _fixture = new Fixture();

        [Fact]
        public void Ctor_Task_Created()
        {
            using (var sut = _fixture.GetSut())
            {
                Assert.NotNull(sut.WorkerTask);
            }
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
            var evt = new SentryEvent();
            _fixture.Transport
                .When(t => t.CaptureEventAsync(evt, Arg.Any<CancellationToken>()))
                .Do(_ => signal.Set());

            var sut = _fixture.GetSut();
            sut.EnqueueEvent(evt);

            Assert.True(signal.Wait(TimeSpan.FromSeconds(3)));

            sut.Dispose();

            Assert.Equal(TaskStatus.RanToCompletion, sut.WorkerTask.Status);
        }

        [Fact]
        public void Dispose_EventQueuedZeroShutdownTimeout_CantEmptyQueueBeforeShutdown()
        {
            _fixture.BackgroundWorkerOptions.ShutdownTimeout = default; // Don't wait
            var sync = new AutoResetEvent(false);
            var evt = new SentryEvent();
            using (var sut = _fixture.GetSut())
            {
                _fixture.Transport
                    .When(t => t.CaptureEventAsync(evt, Arg.Any<CancellationToken>()))
                    .Do(p =>
                    {
                        var token = p.ArgAt<CancellationToken>(1);
                        token.ThrowIfCancellationRequested();

                        sync.Set();
                        sync.WaitOne();
                    });

                sut.EnqueueEvent(evt);

                Assert.True(sync.WaitOne(TimeSpan.FromSeconds(2)));

                sut.EnqueueEvent(evt);
                sut.EnqueueEvent(evt);

                sut.Dispose(); // Make sure next round awaits with a cancelled token
                sync.Set();

                Assert.True(sut.WorkerTask.Wait(TimeSpan.FromSeconds(3)));
                // First event was sent, second hit transport with a cancelled token.
                // Third never taken from the queue
                Assert.Single(_fixture.Queue);
            }
        }

        [Fact]
        public void Dispose_EventQueuedDefaultShutdownTimeout_EmptiesQueueBeforeShutdown()
        {
            var sync = new AutoResetEvent(false);
            var evt = new SentryEvent();
            using (var sut = _fixture.GetSut())
            {
                var counter = 0;
                _fixture.Transport
                    .When(t => t.CaptureEventAsync(evt, Arg.Any<CancellationToken>()))
                    .Do(p =>
                    {
                        if (++counter == 2)
                        {
                            var token = p.ArgAt<CancellationToken>(1);
                            Assert.True(token.IsCancellationRequested);
                            throw new OperationCanceledException();
                        }

                        sut.EnqueueEvent(evt);
                        sut.EnqueueEvent(evt);

                        sync.Set();
                        sync.WaitOne();
                    });

                sut.EnqueueEvent(evt);
                Assert.True(sync.WaitOne(TimeSpan.FromSeconds(2)));
                _fixture.CancellationTokenSource.Cancel(); // Make sure next round awaits with a cancelled token
                sync.Set();
                sut.Dispose(); // Since token was already cancelled, it's basically blocking to wait on the task completion

                Assert.Equal(TaskStatus.RanToCompletion, sut.WorkerTask.Status);
                Assert.Empty(_fixture.Queue);
            }
        }

        [Fact]
        public void Create_CancelledTaskAndNoShutdownTimeout_ConsumesNoEvents()
        {
            // Arrange
            _fixture.BackgroundWorkerOptions.ShutdownTimeout = default;
            _fixture.CancellationTokenSource.Cancel();

            // Act
            using (var sut = _fixture.GetSut())
            {
                // Make sure task has finished
                Assert.True(sut.WorkerTask.Wait(TimeSpan.FromSeconds(3)));

                // Assert
                Assert.Equal(TaskStatus.RanToCompletion, sut.WorkerTask.Status);
                sut.Dispose(); // no-op as task is already finished
            }
        }

        [Fact]
        public void CaptureEvent_LimitReached_EventDropped()
        {
            // Arrange
            var expected = new SentryEvent();
            var sync = new AutoResetEvent(false);
            _fixture.BackgroundWorkerOptions.MaxQueueItems = 1;
            _fixture.Transport
                .When(t => t.CaptureEventAsync(expected, Arg.Any<CancellationToken>()))
                .Do(p =>
                {
                    sync.Set(); // Processing first event
                    sync.WaitOne(); // Stay blocked while test queue events
                });

            using (var sut = _fixture.GetSut())
            {
                // Act
                sut.EnqueueEvent(expected);
                sync.WaitOne(); // Wait first event to be in-flight

                var queued = sut.EnqueueEvent(expected);
                Assert.True(queued); // Queue to limit (1)
                queued = sut.EnqueueEvent(expected);
                Assert.False(queued); // Fails to queue second

                sync.Set();
            }
        }

        [Fact]
        public void CaptureEvent_DisposedWorker_ThrowsObjectDisposedException()
        {
            // Arrange
            var expected = new SentryEvent();
            using (var sut = _fixture.GetSut())
            {
                sut.Dispose();

                Assert.Throws<ObjectDisposedException>(() => sut.EnqueueEvent(expected));
            }
        }

        [Fact]
        public void CaptureEvent_NullEvent_ReturnsFalse()
        {
            using (var sut = _fixture.GetSut())
            {
                Assert.False(sut.EnqueueEvent(null));
            }
        }

        [Fact]
        public void CaptureEvent_InnerTransportInvoked()
        {
            // Arrange
            var expected = new SentryEvent();
            var sut = _fixture.GetSut();

            // Act
            var queued = sut.EnqueueEvent(expected);
            sut.Dispose();

            // Assert
            Assert.True(queued);
            _fixture.Transport.Received(1).CaptureEventAsync(expected, Arg.Any<CancellationToken>());
        }

        [Fact]
        public void CaptureEvent_InnerTransportThrows_WorkerSuppresses()
        {
            // Arrange
            var expected = new SentryEvent();

            _fixture.Transport
                .When(e => e.CaptureEventAsync(expected))
                .Do(_ => throw new Exception("Sending to sentry failed."));

            using (var sut = _fixture.GetSut())
            {
                // Act
                var queued = sut.EnqueueEvent(expected);

                // Assert
                Assert.True(queued);

                // TODO: tap into exception handling (some ILogger or Action<Exception>)
            }
        }

        [Fact]
        public void QueuedItems_StartsEmpty()
        {
            using (var sut = _fixture.GetSut())
            {
                Assert.Equal(0, sut.QueuedItems);
            }
        }

        [Fact]
        public void QueuedItems_ReflectsQueue()
        {
            const int expectedCount = int.MaxValue;
            _fixture.Queue = Substitute.For<IProducerConsumerCollection<SentryEvent>>();
            _fixture.Queue.Count.Returns(expectedCount);
            using (var sut = _fixture.GetSut())
            {
                Assert.Equal(expectedCount, sut.QueuedItems);
            }
        }
    }
}
