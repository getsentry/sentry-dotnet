using System;
#if !NET6_0_OR_GREATER
using System.Threading.Tasks;
#endif
using Sentry.Extensibility;
using Sentry.Internal.Http;

namespace Sentry.Internal
{
    internal class SdkComposer
    {
        private readonly SentryOptions _options;

        public SdkComposer(SentryOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            if (options.Dsn is null)
                throw new ArgumentException("No DSN defined in the SentryOptions");
        }

        private ITransport CreateTransport()
        {
            // Override for tests
            if (_options.Transport is not null)
            {
                return _options.Transport;
            }

            if (_options.SentryHttpClientFactory is { })
            {
                _options.LogDebug(
                    "Using ISentryHttpClientFactory set through options: {0}.",
                    _options.SentryHttpClientFactory.GetType().Name);
            }

            var httpClientFactory = _options.SentryHttpClientFactory ?? new DefaultSentryHttpClientFactory();
            var httpClient = httpClientFactory.Create(_options);

            var httpTransport = new HttpTransport(_options, httpClient);

            // Non-caching transport
            if (string.IsNullOrWhiteSpace(_options.CacheDirectoryPath))
            {
                return httpTransport;
            }

            // Caching transport
            var cachingTransport = CachingTransport.Create(httpTransport, _options);

            BlockCacheFlush(cachingTransport);

            return cachingTransport;
        }

        internal void BlockCacheFlush(IFlushableTransport transport)
        {
            // If configured, flush existing cache
            if (_options.InitCacheFlushTimeout > TimeSpan.Zero)
            {
                _options.LogDebug(
                    "Flushing existing cache during transport activation up to {0}.",
                    _options.InitCacheFlushTimeout);

                try
                {
                    // Flush cache but block on it only for a limited amount of time.
                    // If we don't flush it in time, then let it continue to run on the
                    // background but don't block the calling thread any more than set timeout.
#if NET6_0_OR_GREATER
                    transport.FlushAsync().WaitAsync(_options.InitCacheFlushTimeout)
                        // Block calling thread (Init) until either Flush or Timeout is reached
                        .GetAwaiter().GetResult();
#else
                    var timeoutTask = Task.Delay(_options.InitCacheFlushTimeout);
                    var flushTask = transport.FlushAsync();

                    // If flush finished in time, finalize the task by awaiting it to
                    // propagate potential exceptions.
                    if (Task.WhenAny(timeoutTask, flushTask).GetAwaiter().GetResult() == flushTask)
                    {
                        flushTask.GetAwaiter().GetResult();
                    }
                    // If flush timed out, log and continue
                    else
                    {
                        _options.LogInfo(
                            "Cache flushing is taking longer than the configured timeout of {0}. " +
                            "Continuing without waiting for the task to finish.",
                            _options.InitCacheFlushTimeout);
                    }
#endif
                }
                catch (Exception ex)
                {
                    _options.LogError(
                        "Cache flushing failed.",
                        ex);
                }
            }
        }

        public IBackgroundWorker CreateBackgroundWorker()
        {
            if (_options.BackgroundWorker is { } worker)
            {
                _options.LogDebug("Using IBackgroundWorker set through options: {0}.",
                    worker.GetType().Name);

                return worker;
            }

            var transport = CreateTransport();

            return new BackgroundWorker(transport, _options);
        }
    }
}
