using Sentry.Internal;

namespace Sentry.Integrations
{
    internal class DuplicateEventDetectionIntegration : ISdkIntegration
    {
        public void Register(IHub hub, SentryOptions options)
        {
            hub.ConfigureScope(s => s.EventProcessors.Add(new DuplicateEventDetectionEventProcessor(options)));
        }
    }
}
