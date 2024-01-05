using Sentry.Internal.Http;
using BackgroundWorker = Sentry.Internal.BackgroundWorker;

namespace Sentry.Coyote.Tests;

public static class BackgroundWorkerTests
{
    [Microsoft.Coyote.SystematicTesting.Test]
    public static async Task EnqueueFlushAndDisposeAsync()
    {
        var path = Path.Combine("envelopes", Guid.NewGuid().ToString());
        var options = new SentryOptions
        {
            Dsn = "https://test@testsentry.dev/id",
            CacheDirectoryPath = path,
        };
        var bg = new BackgroundWorker(CachingTransport.Create(new NoOpTransport(), options), options);

        await bg.FlushAsync(TimeSpan.FromSeconds(3));
        bg.EnqueueEnvelope(Envelope.FromEvent(new SentryEvent()));
        bg.EnqueueEnvelope(Envelope.FromEvent(new SentryEvent()));
        await bg.FlushAsync(TimeSpan.FromSeconds(3));
        bg.EnqueueEnvelope(Envelope.FromEvent(new SentryEvent()));
        bg.Dispose();
        Directory.Delete(path);
    }

    private class NoOpTransport : ITransport
    {
        public System.Threading.Tasks.Task SendEnvelopeAsync(
            Envelope envelope,
            CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
