using System;
using System.Diagnostics;

namespace Sentry.Extensions.Logging
{
    /// <summary>
    /// Class that subscribes to specific listeners from DiagnosticListener.
    /// </summary>
    internal class SentryDiagnosticSubscriber : IObserver<DiagnosticListener>
    {
        private SentryEFCoreListener? _efInterceptor { get; set; }

        private IHub _hub { get; }
        private SentryOptions _options { get; }

        public SentryDiagnosticSubscriber(IHub hub, SentryOptions options)
        {
            _hub = hub;
            _options = options;
        }

        public void OnCompleted() { }

        public void OnError(Exception error) { }

        public void OnNext(DiagnosticListener listener)
        {
            if (listener.Name == "Microsoft.EntityFrameworkCore")
            {
                _efInterceptor = new(_hub, _options);
                listener.Subscribe(_efInterceptor);
            }
        }
    }
}
