using System;
using System.Runtime.ExceptionServices;
using System.Security;
using System.Threading.Tasks;

namespace Sentry.Internal
{
    internal interface IAppDomain
    {
        event UnhandledExceptionEventHandler UnhandledException;

        event EventHandler ProcessExit;

        event EventHandler<UnobservedTaskExceptionEventArgs> UnobservedTaskException;
    }

    internal sealed class AppDomainAdapter : IAppDomain
    {
        public static AppDomainAdapter Instance { get; } = new();

        private AppDomainAdapter()
        {
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        }

        public event UnhandledExceptionEventHandler? UnhandledException;

        public event EventHandler? ProcessExit;

        public event EventHandler<UnobservedTaskExceptionEventArgs>? UnobservedTaskException;

        private void OnProcessExit(object? sender, EventArgs e) => ProcessExit?.Invoke(sender, e);

        [HandleProcessCorruptedStateExceptions, SecurityCritical]
        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e) => UnhandledException?.Invoke(this, e);

        [HandleProcessCorruptedStateExceptions, SecurityCritical]
        private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e) => UnobservedTaskException?.Invoke(this, e);
    }
}
