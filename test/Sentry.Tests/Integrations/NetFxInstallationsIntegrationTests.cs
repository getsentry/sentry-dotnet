#if NETFX
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sentry.Integrations;
using Sentry.PlatformAbstractions;
using Xunit;

namespace Sentry.Tests.Integrations
{
    public class NetFxInstallationsIntegrationTests
    {
        [SkippableFact]
        public void Register_IgnoredIfPlattformIsMono()
        {
            Skip.If(!Runtime.Current.IsMono());

            //Arrance
            var options = new SentryOptions();
            var integration = new NetFxInstallationsIntegration();

            //Act
            integration.Register(null, options);

            //Assert
            Assert.DoesNotContain(options.EventProcessors, p => p.GetType() == typeof(NetFxInstallationsEventProcessor));
        }

        [SkippableFact]
        public void Register_AddEventProcessorIfNotMono()
        {
            Skip.If(Runtime.Current.IsMono());

            //Arrance
            var options = new SentryOptions();
            var integration = new NetFxInstallationsIntegration();

            //Act
            integration.Register(null, options);

            //Assert
            Assert.Contains(options.EventProcessors, p => p.GetType() == typeof(NetFxInstallationsEventProcessor));
        }
    }
}
#endif
