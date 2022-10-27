#if NET461
using Sentry.Extensibility;
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
            catch (Exception ex)
            {
                options.LogError("Failed to register NetFxInstallations.", ex);
            }
        }
    }
}
#endif
