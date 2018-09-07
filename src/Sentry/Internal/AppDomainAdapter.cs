using System;

namespace Sentry.Internal
{
    internal interface IAppDomain
    {
        event UnhandledExceptionEventHandler UnhandledException;
    }

    internal sealed class AppDomainAdapter : IAppDomain
    {
        public static AppDomainAdapter Instance { get; } = new AppDomainAdapter();

        private AppDomainAdapter() => AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

        public event UnhandledExceptionEventHandler UnhandledException;

        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
            => UnhandledException?.Invoke(this, e);
    }
}
