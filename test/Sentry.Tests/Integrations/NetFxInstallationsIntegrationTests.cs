#if NET461
using Sentry.Integrations;
using Sentry.PlatformAbstractions;
using Xunit;

namespace Sentry.Tests.Integrations
{
    public class NetFxInstallationsIntegrationTests
    {
        [SkippableFact]
        public void Register_CurrentRuntimeIsMono_NetFxInstallationsEventProcessorNotAdded()
        {
            Skip.If(!Runtime.Current.IsMono());

            //Arrange
            var options = new SentryOptions();
            var integration = new NetFxInstallationsIntegration();

            //Act
            integration.Register(null!, options);

            //Assert
            Assert.DoesNotContain(options.EventProcessors!, p => p.GetType() == typeof(NetFxInstallationsEventProcessor));
        }

        [SkippableFact]
        public void Register_CurrentRuntimeIsNotMono_NetFxInstallationsEventProcessorAdded()
        {
            Skip.If(Runtime.Current.IsMono());

            //Arrange
            var options = new SentryOptions();
            var integration = new NetFxInstallationsIntegration();

            //Act
            integration.Register(null!, options);

            //Assert
            Assert.Contains(options.EventProcessors!, p => p.GetType() == typeof(NetFxInstallationsEventProcessor));
        }
    }
}
#endif
