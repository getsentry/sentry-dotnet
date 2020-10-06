using System;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Sentry.Extensibility;
using Sentry.Internal;

namespace Sentry.Benchmarks
{
    public class BackgroundWorkerFlushBenchmarks
    {
        private class FakeTransport : ITransport
        {
            public Task CaptureEventAsync(
                SentryEvent @event,
                CancellationToken cancellationToken = default)
                => Task.CompletedTask;
        }

        private IBackgroundWorker _backgroundWorker;
        private SentryEvent _event;

        [IterationSetup]
        public void IterationSetup()
        {
            _backgroundWorker = new BackgroundWorker(new FakeTransport(), new SentryOptions { MaxQueueItems = 1000 });
            _event = new SentryEvent();
            // Make sure worker spins once.
            _backgroundWorker.EnqueueEnvelope(_event);
            _backgroundWorker.FlushAsync(TimeSpan.FromSeconds(10)).GetAwaiter().GetResult();
            for (var i = 0; i < Items; i++)
            {
                _backgroundWorker.EnqueueEnvelope(_event);
            }
        }

        [Params(1, 10, 100, 1000)]
        public int Items;

        [Benchmark(Description = "Enqueue event and FlushAsync")]
        public Task FlushAsync_QueueDepthAsync() => _backgroundWorker.FlushAsync(TimeSpan.FromSeconds(10));
    }
}
