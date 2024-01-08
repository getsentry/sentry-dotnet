#if NETFRAMEWORK
using Sentry.PlatformAbstractions;

namespace Sentry.Tests.PlatformAbstractions;

public class NetFxInstallationsEventProcessorTests
{
    private class Fixture
    {
        public SentryOptions SentryOptions { get; set; } = new();

        public NetFxInstallationsEventProcessor GetSut() => new(SentryOptions);
    }

    private readonly Fixture _fixture = new();

    [SkippableFact]
    public void Process_SentryEventWithNetFxList()
    {
        Skip.If(SentryRuntime.Current.IsMono(), "Mono not supported.");

        //Arrange
        var @event = new SentryEvent();
        var sut = _fixture.GetSut();

        //Act
        _ = sut.Process(@event);

        //Assert
        _ = Assert.IsAssignableFrom<Dictionary<string, string>>(@event.Contexts[NetFxInstallationsEventProcessor.NetFxInstallationsKey]);
    }

    [SkippableFact]
    public void Process_ContextWithGetInstallationsData()
    {
        Skip.If(SentryRuntime.Current.IsMono(), "Mono not supported.");

        //Arrange
        var @event = new SentryEvent();
        var sut = _fixture.GetSut();
        var installationList = FrameworkInfo.GetInstallations();
        //Act
        _ = sut.Process(@event);

        //Assert
        var dictionary = (Dictionary<string, string>) @event.Contexts[NetFxInstallationsEventProcessor.NetFxInstallationsKey];
        foreach (var item in installationList)
        {
            Assert.Contains($"\"{item.GetVersionNumber()}\"", dictionary[$"{NetFxInstallationsEventProcessor.NetFxInstallationsKey} {item.Profile}"]);
        }
    }

    [SkippableFact]
    public void Process_NetFxInstallationsKeyExist_UnchangedSentryEvent()
    {
        Skip.If(SentryRuntime.Current.IsMono(), "Mono not supported.");

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
#endif
