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
    public class MemoryQueueTransportTests
    {
        private class Fixture
        {
            public ITransport Transport { get; set; } = Substitute.For<ITransport>();
            public IProducerConsumerCollection<SentryEvent> Queue { get; set; } = Substitute.For<IProducerConsumerCollection<SentryEvent>>();
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
        public void Ctor_Queue_Pooled()
        {
            var evt = new ManualResetEventSlim(false);
            _fixture.Queue.Count.Returns(1);
            _fixture.Queue
                .When(p => p.TryTake(out _))
                .Do(_ => evt.Set());

            using (_fixture.GetSut())
            {
                Assert.True(evt.Wait(TimeSpan.FromSeconds(3)));
            }
        }

        [Fact]
        public void Dispose_StopsTask()
        {
            _fixture.BackgroundWorkerOptions.ShutdownTimeout = default;
            var sut = _fixture.GetSut();
            sut.Dispose();

            Assert.Equal(TaskStatus.RanToCompletion, sut.WorkerTask.Status);
        }

        [Fact]
        public void Create_CancelledTaskAndNoShutdownTimeout_ConsumesNoEvents()
        {
            // Arrange
            _fixture.BackgroundWorkerOptions.ShutdownTimeout = default;
            _fixture.CancellationTokenSource.Cancel();

            // Act
            var sut = _fixture.GetSut();
            // Make sure task has finished
            Assert.True(sut.WorkerTask.Wait(TimeSpan.FromSeconds(3)));

            // Assert
            Assert.Equal(TaskStatus.RanToCompletion, sut.WorkerTask.Status);
            sut.Dispose(); // no-op as task is already finished
        }

        [Fact]
        public void CaptureEvent_InnerTransportInvoked()
        {
            // Arrange
            var expected = new SentryEvent();
            _fixture.Queue = new ConcurrentQueue<SentryEvent>();
            var sut = _fixture.GetSut();

            // Act
            sut.EnqueueEvent(expected);
            sut.Dispose();

            // Assert
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

            _fixture.Queue.Count.Returns(1);
            _fixture.Queue.TryAdd(expected)
                .Returns(true);

            var sut = _fixture.GetSut();

            // Act
            sut.EnqueueEvent(expected);

            // Assert

            // TODO: tap into exception handling (some ILogger or Action<Exception>)
        }
    }
}
