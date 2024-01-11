#if NETFRAMEWORK
using Sentry.PlatformAbstractions;

namespace Sentry.Tests.Integrations;

public class NetFxInstallationsIntegrationTests
{
    [SkippableFact]
    public void Register_CurrentRuntimeIsMono_NetFxInstallationsEventProcessorNotAdded()
    {
        Skip.If(!SentryRuntime.Current.IsMono());

        //Arrange
        var options = new SentryOptions();
        var integration = new NetFxInstallationsIntegration();

        //Act
        integration.Register(null!, options);

        //Assert
        Assert.DoesNotContain(options.EventProcessors!, p => p.Lazy.Value.GetType() == typeof(NetFxInstallationsEventProcessor));
    }

    [SkippableFact]
    public void Register_CurrentRuntimeIsNotMono_NetFxInstallationsEventProcessorAdded()
    {
        Skip.If(SentryRuntime.Current.IsMono());

        //Arrange
        var options = new SentryOptions();
        var integration = new NetFxInstallationsIntegration();

        //Act
        integration.Register(null!, options);

        //Assert
        Assert.Contains(options.EventProcessors!, p => p.Lazy.Value.GetType() == typeof(NetFxInstallationsEventProcessor));
    }
}

#endif
