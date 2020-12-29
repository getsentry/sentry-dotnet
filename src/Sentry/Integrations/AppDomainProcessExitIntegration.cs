using System;
using Sentry.Internal;

namespace Sentry.Integrations
{
    internal class AppDomainProcessExitIntegration : IInternalSdkIntegration
    {
        private readonly IAppDomain _appDomain;
        private IHub? _hub;

        public AppDomainProcessExitIntegration(IAppDomain? appDomain = null)
        {
            _appDomain = appDomain ?? AppDomainAdapter.Instance;
        }

        public void Register(IHub hub, SentryOptions options)
        {
            _hub = hub;
            _appDomain.ProcessExit += HandleProcessExit;
        }

        public void Unregister(IHub hub)
        {
            _appDomain.ProcessExit -= HandleProcessExit;
            _hub = null;
        }

        internal void HandleProcessExit(object? sender, EventArgs e)
        {
            (_hub as IDisposable)?.Dispose();
        }
    }
}
