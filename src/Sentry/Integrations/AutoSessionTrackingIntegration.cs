using Sentry.Internal;

namespace Sentry.Integrations
{
    internal class AutoSessionTrackingIntegration : IInternalSdkIntegration
    {
        private bool _isSessionStarted;

        public void Register(IHub hub, SentryOptions options)
        {
            if (options.AutoSessionTracking)
            {
                hub.StartSession();
                _isSessionStarted = true;
            }
        }

        public void Unregister(IHub hub)
        {
            if (_isSessionStarted)
            {
                hub.EndSession();
            }
        }
    }
}
