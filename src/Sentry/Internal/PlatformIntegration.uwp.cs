#if WINDOWS_UWP
using System;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Security;
using Sentry.Protocol;
using Windows.UI.Xaml;

namespace Sentry.Internal
{
    internal class PlatformIntegration : IInternalSdkIntegration
    {
        private SentryOptions _options;
        private IHub? _hub;

        public void Register(IHub hub, SentryOptions options)
        {
            _options = options;
            _hub = hub;
            //Sentry default Logger doesn't work with UWP.
            options.AddEventProcessor(new PlatformEventProcessor(options));
            var uwpApplication = Application.Current;
            uwpApplication.UnhandledException += Handle;
        }

        public void Unregister(IHub hub)
        {
            _options.EventProcessors = _options.EventProcessors?.Where(p => p.GetType() != typeof(PlatformEventProcessor)).ToArray();
            _hub = null;
            var uwpApplication = Application.Current;
            uwpApplication.UnhandledException -= Handle;
        }

        [HandleProcessCorruptedStateExceptions, SecurityCritical]
        internal void Handle(object sender, Windows.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            //We need to backup the reference, because the Exception reference last for one access.
            //After that, a new  Exception reference is going to be set into e.Exception.
            var exception = e.Exception;
            if (e != null)
            {
                exception.Data[Mechanism.HandledKey] = e.Handled;
                exception.Data[Mechanism.MechanismKey] = "Application.UnhandledException";
                _ = SentrySdk.CaptureException(exception);
                if (!e.Handled)
                {
                    (_hub as IDisposable)?.Dispose();
                }
            }
        }
    }
}
#endif
