using System;
using System.Diagnostics;

namespace Sentry.Integrations
{
    internal class AppDomainUnhandledExceptionIntegration : ISdkIntegration
    {
        private readonly IAppDomain _appDomain;
        private IHub _hub;

        public AppDomainUnhandledExceptionIntegration(IAppDomain appDomain = null)
            => _appDomain = appDomain ?? AppDomainAdapter.Instance;

        public void Register(IHub hub)
        {
            Debug.Assert(hub != null);
            _hub = hub;
            _appDomain.UnhandledException += Handle;
        }

        public void Unregister(IHub hub)
        {
            _appDomain.UnhandledException -= Handle;
            _hub = null;
        }

        // Internal for testability
        internal void Handle(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                _hub.CaptureException(ex);
            }

            if (e.IsTerminating)
            {
                (_hub as IDisposable)?.Dispose();
            }
        }
    }
}
