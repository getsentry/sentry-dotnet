using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sentry
{
    ///
    public class HttpSentryClient : ISentryClient, IDisposable
    {
        ///
        public HttpSentryClient()
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            // TODO: A proper implementation
            CaptureEventAsync(new SentryEvent(e.ExceptionObject as Exception));
        }

        ///
        public Task<SentryResponse> CaptureEventAsync(SentryEvent @event, CancellationToken cancellationToken = default)
            => Task.FromResult(new SentryResponse(false));

        ///
        public SentryResponse CaptureEvent(SentryEvent @event) => new SentryResponse(false);

        ///
        public void Dispose()
        {
            AppDomain.CurrentDomain.UnhandledException -= CurrentDomain_UnhandledException;
        }
    }
}
