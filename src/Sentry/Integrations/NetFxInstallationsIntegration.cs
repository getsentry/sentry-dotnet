#if NETFX
using Sentry.Internal;

namespace Sentry.Integrations
{
    internal class NetFxInstallationsIntegration : ISdkIntegration
    {
        public void Register(IHub hub, SentryOptions options)
        {
            options.AddEventProcessor(new NetFxInstallationsEventProcessor());
        }
    }
}
#endif
