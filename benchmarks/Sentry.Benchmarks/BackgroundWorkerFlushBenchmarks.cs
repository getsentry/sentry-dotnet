using System;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Sentry.Extensibility;
using Sentry.Internal;
using Sentry.Protocol.Envelopes;

namespace Sentry.Benchmarks
{
    public class BackgroundWorkerFlushBenchmarks
    {
        private class FakeTransport : ITransport
        {
            public Task SendEnvelopeAsync(Envelope envelope, CancellationToken cancellationToken = default)
                => default;
        }

        private IBackgroundWorker _backgroundWorker;
        private SentryEvent _event;
        private Envelope _envelope;

        [IterationSetup]
        public void IterationSetup()
        {
            _backgroundWorker = new BackgroundWorker(new FakeTransport(), new SentryOptions { MaxQueueItems = 1000 });
            _event = new SentryEvent();
            _envelope = Envelope.FromEvent(_event);

            // Make sure worker spins once.
            _backgroundWorker.EnqueueEnvelope(_envelope);
            _backgroundWorker.FlushAsync(TimeSpan.FromSeconds(10)).GetAwaiter().GetResult();
            for (var i = 0; i < Items; i++)
            {
                _backgroundWorker.EnqueueEnvelope(_envelope);
            }
        }

        [Params(1, 10, 100, 1000)]
        public int Items;

        [Benchmark(Description = "Enqueue event and FlushAsync")]
        public Task FlushAsync_QueueDepthAsync() => _backgroundWorker.FlushAsync(TimeSpan.FromSeconds(10));
    }
}
