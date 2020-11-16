using System;
using Sentry.Internal;
using System.Runtime.ExceptionServices;
using System.Security;
using Sentry.Protocol;
using System.Threading.Tasks;

namespace Sentry.Integrations
{
    internal class TaskUnobservedTaskExceptionIntegration : IInternalSdkIntegration
    {
        private readonly IAppDomain _appDomain;
        private IHub? _hub;

        internal TaskUnobservedTaskExceptionIntegration(IAppDomain? appDomain = null)
            => _appDomain = appDomain ?? AppDomainAdapter.Instance;

        public void Register(IHub hub, SentryOptions _)
        {
            _hub = hub;
            _appDomain.UnobservedTaskException += Handle;
        }

        public void Unregister(IHub hub)
        {
            _appDomain.UnobservedTaskException -= Handle;
            _hub = null;
        }

        // Internal for testability
        [HandleProcessCorruptedStateExceptions, SecurityCritical]
        internal void Handle(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            if (e.Exception != null)
            {
                e.Exception.Data[Mechanism.MechanismKey] = "UnobservedTaskException";
                _ = _hub?.CaptureException(e.Exception);
                (_hub as IDisposable)?.Dispose();
            }
        }
    }
}
