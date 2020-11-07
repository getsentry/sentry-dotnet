#if WINDOWS_UWP
using System;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Security;
using Sentry.Extensibility;
using Sentry.Protocol;
using Windows.UI.Xaml;

namespace Sentry.Internal
{
    internal class Logger : IDiagnosticLogger
    {
        public bool IsEnabled(SentryLevel level) => true;

        public void Log(SentryLevel logLevel, string message, Exception? exception = null, params object?[] args)
        {
            global::System.Diagnostics.Debug.WriteLine($"Sentry UWP Logger - {logLevel} {message} {exception?.Message}");
        }
    }
    internal class PlatformIntegration : IInternalSdkIntegration
    {
        private SentryOptions _options;
        public void Register(IHub hub, SentryOptions options)
        {
            _options = options;
            //Sentry default Logger doesn't work with UWP.
            options.DiagnosticLogger = new Logger();
            options.AddEventProcessor(new PlatformEventProcessor(options));
            var uwpApplication = Application.Current;
            uwpApplication.UnhandledException += Handle;

        }

        public void Unregister(IHub hub)
        {
            _options.EventProcessors = _options.EventProcessors?.Where(p => p.GetType() != typeof(PlatformEventProcessor)).ToArray();
            var uwpApplication = Application.Current;
            uwpApplication.UnhandledException -= Handle;
        }

        [HandleProcessCorruptedStateExceptions, SecurityCritical]
        internal void Handle(object sender, Windows.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            //We need to backup the reference, because the Exception reference last for one access.
            //After that, a new  Exception reference is going to be set into e.Exception.
            var exception = e.Exception;
            if (e.Exception != null)
            {
                exception.Data[Mechanism.HandledKey] = e.Handled;
                exception.Data[Mechanism.MechanismKey] = "Application.UnhandledException";
                _ = SentrySdk.CaptureException(exception);
                if (!e.Handled)
                {
                    // App might crash so make sure we flush this event.
                    SentrySdk.FlushAsync(TimeSpan.FromSeconds(10)).GetAwaiter().GetResult();
                    SentrySdk.Close();
                }
            }
        }
    }
}
#endif
