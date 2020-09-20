using System;
using System.Diagnostics;
using Sentry.Internal;
using System.Runtime.ExceptionServices;
using System.Security;
using Sentry.Protocol;

namespace Sentry.Integrations
{
    internal class AppDomainUnhandledExceptionIntegration : IInternalSdkIntegration
    {
        private readonly IAppDomain _appDomain;
        private IHub? _hub;

        internal AppDomainUnhandledExceptionIntegration(IAppDomain? appDomain = null)
            => _appDomain = appDomain ?? AppDomainAdapter.Instance;

        public void Register(IHub hub, SentryOptions _)
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
        [HandleProcessCorruptedStateExceptions, SecurityCritical]
        internal void Handle(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                ex.Data[Mechanism.HandledKey] = false;
                ex.Data[Mechanism.MechanismKey] = "AppDomain.UnhandledException";

#if RELEASE && __MOBILE__
                if (_hub is Hub h)
                {
                    var evt = h.PrepareEvent(new SentryEvent(ex));
                    // Mono AOT crashes before you can block for submission on a background thread
                    // Blocking on a async I/O works correctly though
                    // Work around to grab the transport and make sure we bypass the background worker
                    (h._ownedClient.Worker as BackgroundWorker)?._transport.CaptureEventAsync(evt).Wait();
                }
#else
                _ = _hub?.CaptureException(ex);
#endif
            }

            if (e.IsTerminating)
            {
                (_hub as IDisposable)?.Dispose();
            }
        }
    }
}
