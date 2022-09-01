namespace Sentry.AspNetCore.Tests;

public class AspNetCoreEventProcessorTests
{
    private readonly AspNetCoreEventProcessor _sut;

    public AspNetCoreEventProcessorTests()
    {
        _sut = new AspNetCoreEventProcessor();
    }

    [Fact]
    public void Process_ServerName_NotOverwritten()
    {
        var target = new SentryEvent();
        const string expectedServerName = "original";
        target.ServerName = expectedServerName;

        _ = _sut.Process(target);

        Assert.Equal(expectedServerName, target.ServerName);
    }

    [Fact]
    public void Process_ServerName_SetToEnvironmentMachineName()
    {
        var target = new SentryEvent();

        _ = _sut.Process(target);

        Assert.Equal(Environment.MachineName, target.ServerName);
    }
}
