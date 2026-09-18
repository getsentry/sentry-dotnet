using Microsoft.Extensions.Logging;

namespace Sentry.Extensions.Logging.Tests;

public class SentryLoggerFactoryExtensionsTests
{
    [Fact]
    public void AddSentry_ProviderUsesHubAdapter()
    {
        var sut = Substitute.For<ILoggerFactory>();

        _ = sut.AddSentry();

        sut.Received(1)
            .AddProvider(Arg.Is<SentryLoggerProvider>(p => p.Hub == HubAdapter.Instance));
    }

    [Fact]
    public void AddSentry_WithOptionsCallback_CallbackInvoked()
    {
        var callbackInvoked = false;
        var expected = Substitute.For<ILoggerFactory>();
        _ = expected.AddSentry(o =>
        {
            o.MinimumEventLevel = LogLevel.Critical;
            callbackInvoked = true;
        });

        Assert.True(callbackInvoked);
    }

    [Fact]
    public void AddSentry_NoOptionsDelegate_ProviderAdded()
    {
        var expected = Substitute.For<ILoggerFactory>();
        _ = expected.AddSentry();

        expected.Received(1)
            .AddProvider(Arg.Is<ILoggerProvider>(p => p is SentryLoggerProvider));
    }

    [Fact]
    public void AddSentry_ReturnsSameFactory()
    {
        var expected = Substitute.For<ILoggerFactory>();
        var actual = expected.AddSentry(o => o.MinimumEventLevel = LogLevel.Critical);

        Assert.Same(expected, actual);
    }

    [Fact]
    public void AddSentry_ConfigureOptionsOverload_ReturnsSameFactory()
    {
        var expected = Substitute.For<ILoggerFactory>();
        var actual = expected.AddSentry(o => o.MinimumEventLevel = LogLevel.Critical);

        Assert.Same(expected, actual);
    }

    [Fact]
    public void AddSentry_ConfigureOptionsOverload_InvokesCallback()
    {
        var expected = Substitute.For<ILoggerFactory>();

        var invoked = false;
        _ = expected.AddSentry(o =>
        {
            o.MinimumEventLevel = LogLevel.Critical;
            Assert.NotNull(o);
            invoked = true;
        });

        Assert.True(invoked);
    }

    [Fact]
    public void Namespace_MicrosoftExtensionsLogging()
    {
        var @namespace = typeof(SentryLoggerFactoryExtensions).Namespace;

        Assert.Equal("Microsoft.Extensions.Logging", @namespace);
    }
}
