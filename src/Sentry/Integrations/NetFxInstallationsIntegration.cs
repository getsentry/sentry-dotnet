#if NET461
using Sentry.PlatformAbstractions;

namespace Sentry.Integrations
{
    internal class NetFxInstallationsIntegration : ISdkIntegration
    {
        public void Register(IHub hub, SentryOptions options)
        {
            try
            {
                if (!Runtime.Current.IsMono())
                {
                    options.AddEventProcessor(new NetFxInstallationsEventProcessor(options));
                }
            }
            catch
            {
                // This can fail due to Xyz so let's just live without this information and let the SDK initialize
            }
        }
    }
}
#endif
