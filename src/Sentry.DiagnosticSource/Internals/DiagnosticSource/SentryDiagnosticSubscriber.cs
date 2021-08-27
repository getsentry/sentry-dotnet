using System;
using System.Collections.Generic;
using System.Diagnostics;
using Sentry.Extensibility;

namespace Sentry.Internals.DiagnosticSource
{
    /// <summary>
    /// Class that subscribes to specific listeners from DiagnosticListener.
    /// </summary>
    internal class SentryDiagnosticSubscriber : IObserver<DiagnosticListener>, IDisposable
    {
        private SentryEFCoreListener? _efInterceptor { get; set; }
        private SentrySqlListener? _sqlListener { get; set; }
        private List<IDisposable> _disposableListeners = new();
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
                _disposableListeners.Add(listener.Subscribe(_efInterceptor));
                _options.DiagnosticLogger?.Log(SentryLevel.Debug, "Registered integration with EF Core.");
            }
            else if (listener.Name == "SqlClientDiagnosticListener")
            {
                _sqlListener = new(_hub, _options);
                _disposableListeners.Add(listener.Subscribe(_sqlListener));
                _options.DiagnosticLogger?.LogDebug("Registered integration with SQL Client.");

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
