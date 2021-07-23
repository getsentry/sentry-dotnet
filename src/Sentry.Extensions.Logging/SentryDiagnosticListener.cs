using System;
using System.Diagnostics;

namespace Sentry.Extensions.Logging
{
    /// <summary>
    /// Class that subscribes to specific listeners from DiagnosticListener.
    /// </summary>
    internal class SentryDiagnosticListener : IObserver<DiagnosticListener>
    {
        private SentryEFCoreInterceptor? _efInterceptor { get; set; }

        private IHub _hub { get; }

        public SentryDiagnosticListener(IHub hub) => _hub = hub;
        public void OnCompleted() { }

        public void OnError(Exception error) { }

        public void OnNext(DiagnosticListener listener)
        {
            if (listener.Name == "Microsoft.EntityFrameworkCore" && _efInterceptor == null)
            {
                _efInterceptor = new(_hub);
                listener.Subscribe(_efInterceptor);
            }
        }
    }
}
