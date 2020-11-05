using System;
using System.IO;
using System.Threading.Tasks;
using Sentry.Extensibility;
using Sentry.Protocol.Envelopes;

namespace Sentry.Internal
{
    internal class CachingBackgroundWorker : IBackgroundWorker, IDisposable
    {
        private readonly ITransport _transport;
        private readonly SentryOptions _options;
        private readonly DirectoryInfo _cacheDirectory;

        public int QueuedItems { get; }

        public CachingBackgroundWorker(ITransport transport, SentryOptions options, DirectoryInfo cacheDirectory)
        {
            _transport = transport;
            _options = options;
            _cacheDirectory = cacheDirectory;
        }

        public bool EnqueueEnvelope(Envelope envelope)
        {
            throw new NotImplementedException();
        }

        public ValueTask FlushAsync(TimeSpan timeout)
        {
            throw new NotImplementedException();
        }


        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
