using System;

namespace Sentry.Integrations
{
    internal interface IAppDomain
    {
        event UnhandledExceptionEventHandler UnhandledException;
    }

    internal class AppDomainAdapter : IAppDomain
    {
        public static AppDomainAdapter Instance { get; } = new AppDomainAdapter();

        private AppDomainAdapter()
        {
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        }

        public event UnhandledExceptionEventHandler UnhandledException;

        protected virtual void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
            => UnhandledException?.Invoke(this, e);
    }
}
