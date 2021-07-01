#if NET461
using Sentry.PlatformAbstractions;

namespace Sentry.Integrations
{
    internal class NetFxInstallationsIntegration : ISdkIntegration
    {
        public void Register(IHub hub, SentryOptions options)
        {
            if (!Runtime.Current.IsMono() && FrameworkInfo.RegistryAccessAllowed)
            {
                options.AddEventProcessor(new NetFxInstallationsEventProcessor(options));
            }
        }
    }
}
#endif
