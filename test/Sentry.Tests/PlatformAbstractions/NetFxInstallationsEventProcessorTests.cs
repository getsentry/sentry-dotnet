#if NETFX
using Xunit;
using System.Collections.Generic;
using Sentry.PlatformAbstractions;
using Xunit.Abstractions;
using Sentry.Extensibility;
using System;

namespace Sentry.Tests.PlatformAbstractions
{

    public class NetFxInstallationsEventProcessorTests
    {

        private class TestLogger : IDiagnosticLogger
        {
            public bool IsEnabled(SentryLevel level) => true;

            public void Log(SentryLevel logLevel, string message, Exception exception = null, params object[] args)
            {
                _testOutputHelper.WriteLine($"sentry IDiagnosticLogger {logLevel} {message}, ex {exception?.Message} {exception?.StackTrace}");
            }
        }

        private class Fixture
        {

            public SentryOptions SentryOptions { get; set; } = new SentryOptions()
            {
                Debug = true,
                DiagnosticLogger = new TestLogger()
            };

            public NetFxInstallationsEventProcessor GetSut() => new NetFxInstallationsEventProcessor(SentryOptions);
        }
        private  static ITestOutputHelper _testOutputHelper;

        public NetFxInstallationsEventProcessorTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
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
            _ = Assert.IsAssignableFrom<Dictionary<string,string>>(@event.Contexts[NetFxInstallationsEventProcessor.NetFxInstallationsKey]);
        }

        [Fact]
        public void Process_ContextWithGetInstallationsData()
        {
            //Arrange
            var @event = new SentryEvent();
            var sut = _fixture.GetSut();
            var installationList = FrameworkInfo.GetInstallations();
            //Act
            _ = sut.Process(@event);

            //Assert
            var dictionary = @event.Contexts[NetFxInstallationsEventProcessor.NetFxInstallationsKey] as Dictionary<string, string>;
            foreach(var item in installationList)
            {
                Assert.Contains($"\"{item.GetVersionNumber()}\"", dictionary[$"{NetFxInstallationsEventProcessor.NetFxInstallationsKey} {item.Profile}"]);
            }
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
