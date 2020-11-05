#if NETFX
using Sentry.PlatformAbstractions;

namespace Sentry.Integrations
{
    internal class NetFxInstallationsIntegration : ISdkIntegration
    {
        public void Register(IHub hub, SentryOptions options)
        {
            if (!Runtime.Current.IsMono())
            {
                options.AddEventProcessor(new NetFxInstallationsEventProcessor(options));
            }
        }
    }
}
#endif
