namespace Sentry.Integrations
{
    internal class AutoSessionTrackingIntegration : ISdkIntegration
    {
        public void Register(IHub hub, SentryOptions options)
        {
            if (options.AutoSessionTracking)
            {
                hub.StartSession();
            }
        }
    }
}
