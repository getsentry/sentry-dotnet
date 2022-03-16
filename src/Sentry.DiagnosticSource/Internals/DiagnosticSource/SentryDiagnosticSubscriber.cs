using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using Sentry.Extensibility;

namespace Sentry.Internals.DiagnosticSource
{
    /// <summary>
    /// Class that subscribes to specific listeners from DiagnosticListener.
    /// </summary>
    internal class SentryDiagnosticSubscriber : IObserver<DiagnosticListener>, IDisposable
    {
        private SentryEFCoreListener? _efInterceptor;
        private SentrySqlListener? _sqlListener;
        private ConcurrentBag<IDisposable> _disposableListeners = new();
        private IHub _hub;
        private SentryOptions _options;

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
                _disposableListeners.Add(listener.Subscribe(_efInterceptor));
                _options.Log(SentryLevel.Debug, "Registered integration with EF Core.");
            }
            else if (listener.Name == "SqlClientDiagnosticListener")
            {
                _sqlListener = new(_hub, _options);
                _disposableListeners.Add(listener.Subscribe(_sqlListener));
                _options.LogDebug("Registered integration with SQL Client.");

                // Duplicated data.
                _efInterceptor?.DisableConnectionSpan();
                _efInterceptor?.DisableQuerySpan();
            }
        }

        /// <summary>
        /// Dispose all registered integrations.
        /// </summary>
        public void Dispose()
        {
            foreach (var item in _disposableListeners)
            {
                item.Dispose();
            }
        }
    }
}
