using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Sentry.Internal;

namespace Sentry.Integrations
{
    internal class AppDomainProcessExitIntegration : IInternalSdkIntegration
    {
        private readonly IAppDomain _appDomain;
        private IHub _hub;

        public AppDomainProcessExitIntegration(IAppDomain appDomain = null)
        {
            _appDomain = appDomain ?? AppDomainAdapter.Instance;
        }

        public void Register(IHub hub, SentryOptions options)
        {
            Debug.Assert(hub != null);
            _hub = hub;
            _appDomain.ProcessExit += HandlerAsync;
        }

        public void Unregister(IHub hub)
        {
            _appDomain.ProcessExit -= HandlerAsync;
            _hub = null;
        }

        private static readonly TimeSpan _flushTimeOut = TimeSpan.FromSeconds(1);

        internal async void HandlerAsync(object sender, EventArgs e)
        {
            await FlushAsync(sender, e).ConfigureAwait(false);
        }

        internal async Task FlushAsync(object sender, EventArgs e)
        {
            if (_hub != null)
            {
                await _hub.FlushAsync(_flushTimeOut).ConfigureAwait(false);
                (_hub as IDisposable)?.Dispose();
            }
        }
    }


}
