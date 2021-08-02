using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Sentry.Extensions.Logging
{
    /// <summary>
    /// Class that subscribes to specific listeners from DiagnosticListener.
    /// </summary>
    internal class SentryDiagnosticSubscriber : IObserver<DiagnosticListener>
    {
        private SentryEFCoreListener? _efInterceptor { get; set; }
        private SentrySqlListener? _sqlListener { get; set; }

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
            else if(listener.Name == "SqlClientDiagnosticListener")
            {
                //https://www.nuget.org/packages/Microsoft.Data.SqlClient/3.0.0?_src=template
                //https://www.nuget.org/packages/System.Data.SqlClient/4.8.2?_src=template
                //https://sentry.io/organizations/sentry-sdks/discover/sentry-dotnet:a6f5fbff365f44d59689a6e5b600f297
                //https://sentry.io/organizations/sentry-sdks/discover/sentry-dotnet:08c5d523acd947e0aed046e3fb14569f
                _sqlListener = new(_hub, _options);
                listener.Subscribe(_sqlListener);
            }
        }
    }
}
