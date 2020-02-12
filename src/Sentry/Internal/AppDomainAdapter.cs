using System;
using System.Runtime.ExceptionServices;
using System.Security;

namespace Sentry.Internal
{
    internal interface IAppDomain
    {
        event UnhandledExceptionEventHandler UnhandledException;

        event EventHandler ProcessExit;
    }

    internal sealed class AppDomainAdapter : IAppDomain
    {
        public static AppDomainAdapter Instance { get; } = new AppDomainAdapter();

        private AppDomainAdapter()
        {
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
        }

        public event UnhandledExceptionEventHandler UnhandledException;

        public event EventHandler ProcessExit;

        private void OnProcessExit(object sender, EventArgs e) => ProcessExit?.Invoke(sender, e);

        [HandleProcessCorruptedStateExceptions, SecurityCritical]
        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e) => UnhandledException?.Invoke(this, e);
    }
}
