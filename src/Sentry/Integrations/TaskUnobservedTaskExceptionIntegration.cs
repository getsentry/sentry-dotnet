using System.Runtime.ExceptionServices;
using System.Security;
using System.Threading.Tasks;
using Sentry.Internal;
using Sentry.Protocol;

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
#if !NET6_0_OR_GREATER
        [HandleProcessCorruptedStateExceptions]
#endif
        [SecurityCritical]
        internal void Handle(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            if (e.Exception != null)
            {
                e.Exception.Data[Mechanism.HandledKey] = false;
                e.Exception.Data[Mechanism.MechanismKey] = "UnobservedTaskException";
                _ = _hub?.CaptureException(e.Exception);
            }
        }
    }
}
