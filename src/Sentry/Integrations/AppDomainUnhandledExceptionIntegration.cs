using System;
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
                _ = (_hub?.CaptureException(ex));
            }

            if (e.IsTerminating)
            {
                (_hub as IDisposable)?.Dispose();
            }
        }
    }
}
