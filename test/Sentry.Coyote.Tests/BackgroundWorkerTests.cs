using System;
using System.Threading;
using Microsoft.Coyote.Tasks;
using Sentry.Extensibility;
using Sentry.Internal;
using Sentry.Internal.Http;
using Sentry.Protocol.Envelopes;

namespace Sentry.Coyote.Tests
{
    public static class BackgroundWorkerTests
    {
        [Microsoft.Coyote.SystematicTesting.Test]
        public static async Task EnqueueFlushAndDispose()
        {
            var options = new SentryOptions();
            // TODO: SemaphoreSlim isn't rewritten yet: https://github.com/microsoft/coyote/issues/163
            var bg = new BackgroundWorker(new CachingTransport(new NoOpTransport(), options), options);

            await bg.FlushAsync(TimeSpan.FromSeconds(3));
            bg.EnqueueEnvelope(Envelope.FromEvent(new SentryEvent()));
            bg.EnqueueEnvelope(Envelope.FromEvent(new SentryEvent()));
            await bg.FlushAsync(TimeSpan.FromSeconds(3));
            bg.EnqueueEnvelope(Envelope.FromEvent(new SentryEvent()));
            bg.Dispose();
        }

        private class NoOpTransport : ITransport
        {
            public System.Threading.Tasks.Task SendEnvelopeAsync(
                Envelope envelope,
                CancellationToken cancellationToken = default)
                => System.Threading.Tasks.Task.CompletedTask;
        }
    }
}
