using System;
using System.Diagnostics;

namespace Sentry.Extensions.Logging
{
    /// <summary>
    /// Class that subscribes to specific listeners from DiagnosticListener.
    /// </summary>
    internal class SentryDiagnosticListener : IObserver<DiagnosticListener>
    {
        private readonly SentryEFCoreInterceptor _efInterceptor;

        private IHub _hub { get; }

        public SentryDiagnosticListener(IHub hub) { _hub = hub; _efInterceptor = new(_hub); }
        public void OnCompleted() { }

        public void OnError(Exception error) { }

        public void OnNext(DiagnosticListener listener)
        {
            if (listener.Name == "Microsoft.EntityFrameworkCore")
            {
                listener.Subscribe(_efInterceptor);
            }
        }
    }
}
