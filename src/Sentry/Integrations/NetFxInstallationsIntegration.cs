#if NETFRAMEWORK
using Sentry.Extensibility;
using Sentry.PlatformAbstractions;

namespace Sentry.Integrations;

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
            options.LogError(ex, "Failed to register NetFxInstallations.");
        }
    }
}

#endif
