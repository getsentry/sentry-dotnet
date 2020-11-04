#if NETFX
using Xunit;
using Sentry.Internal;
using System.Collections.Generic;
using Sentry.PlatformAbstractions;

namespace Sentry.Tests.Internals
{
    public class NetFxInstallationsEventProcessorTests
    {
        private class Fixture
        {
            public NetFxInstallationsEventProcessor GetSut() => new NetFxInstallationsEventProcessor();
        }

        private readonly Fixture _fixture = new Fixture();

        [Fact]
        public void Process_SentryEventWithNetFxList()
        {
            //Arrange
            var @event = new SentryEvent();
            var sut = _fixture.GetSut();

            //Act
            _ = sut.Process(@event);

            //Assert
            _ = Assert.IsAssignableFrom<IEnumerable<FrameworkInstallation>>(@event.Contexts[NetFxInstallationsEventProcessor.NetFxInstallationsKey]);
        }

        [Fact]
        public void Process_NetFxInstallationsKeyExist_UnchangedSentryEvent()
        {
            //Arrange
            var @event = new SentryEvent();
            var sut = _fixture.GetSut();
            var userBlob = "user blob";
            @event.Contexts[NetFxInstallationsEventProcessor.NetFxInstallationsKey] = userBlob;

            //Act
            _ = sut.Process(@event);

            //Assert
            Assert.Equal(userBlob, @event.Contexts[NetFxInstallationsEventProcessor.NetFxInstallationsKey]);
        }
    }
}
#endif
