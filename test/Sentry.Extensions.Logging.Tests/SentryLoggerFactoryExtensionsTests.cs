using Microsoft.Extensions.Logging;

namespace Sentry.Extensions.Logging.Tests;

public class SentryLoggerFactoryExtensionsTests
{
    [Fact]
    public void AddSentry_ConfigureScope_HubEnabledTrue_InvokesCallback()
    {
        const SentryLevel expected = SentryLevel.Debug;
        var sut = Substitute.For<ILoggerFactory>();
        var hub = Substitute.For<IHub>();
        _ = hub.IsEnabled.Returns(true);
        var scope = new Scope(new SentryOptions());
        hub.When(w => w.ConfigureScope(Arg.Any<Action<Scope>>()))
            .Do(info => info.Arg<Action<Scope>>()(scope));
        _ = SentrySdk.UseHub(hub);

        _ = sut.AddSentry(o =>
        {
            o.InitializeSdk = false; // use the mock above
            o.ConfigureScope(s => s.Level = expected);
        });

        Assert.Equal(expected, scope.Level);
    }

    [Fact]
    public void AddSentry_ConfigureScope_HubEnabledFalse_DoesNotInvokesCallback()
    {
        const SentryLevel expected = SentryLevel.Debug;
        var sut = Substitute.For<ILoggerFactory>();
        var hub = Substitute.For<IHub>();
        _ = hub.IsEnabled.Returns(false);
        var scope = new Scope(new SentryOptions());
        hub.When(w => w.ConfigureScope(Arg.Any<Action<Scope>>()))
            .Do(info => info.Arg<Action<Scope>>()(scope));
        _ = SentrySdk.UseHub(hub);

        _ = sut.AddSentry(o =>
        {
            o.InitializeSdk = false; // use the mock above
            o.ConfigureScope(s => s.Level = expected);
        });

        Assert.NotEqual(expected, scope.Level);
    }

    [Fact]
    public void AddSentry_InitializeSdkFalse_HubAdapter()
    {
        var sut = Substitute.For<ILoggerFactory>();

        _ = sut.AddSentry(o => o.InitializeSdk = false);

        sut.Received(1)
            .AddProvider(Arg.Is<SentryLoggerProvider>(p => p.Hub == HubAdapter.Instance));
    }

    [Fact]
    public void AddSentry_NoDiagnosticSet_MelSet()
    {
        SentryLoggingOptions options = null;
        var sut = Substitute.For<ILoggerFactory>();
        _ = sut.AddSentry(o =>
        {
            o.Dsn = Sentry.SentryConstants.DisableSdkDsnValue;
            o.Debug = true;
            options = o;
        });

        _ = Assert.IsType<MelDiagnosticLogger>(options.DiagnosticLogger);
    }

    [Fact]
    public void AddSentry_DiagnosticSet_NoOverriden()
    {
        SentryLoggingOptions options = null;
        var sut = Substitute.For<ILoggerFactory>();
        var diagnosticLogger = Substitute.For<IDiagnosticLogger>();
        _ = sut.AddSentry(o =>
        {
            o.Dsn = Sentry.SentryConstants.DisableSdkDsnValue;
            o.Debug = true;
            Assert.Null(o.DiagnosticLogger);
            o.DiagnosticLogger = diagnosticLogger;
            options = o;
        });

        Assert.Same(diagnosticLogger, options.DiagnosticLogger);
    }

    [Fact]
    public void AddSentry_WithOptionsCallback_CallbackInvoked()
    {
        var callbackInvoked = false;
        var expected = Substitute.For<ILoggerFactory>();
        _ = expected.AddSentry(o =>
        {
            o.Dsn = Sentry.SentryConstants.DisableSdkDsnValue;
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
        var actual = expected.AddSentry(o => o.Dsn = Sentry.SentryConstants.DisableSdkDsnValue);

        Assert.Same(expected, actual);
    }

    [Fact]
    public void AddSentry_ConfigureOptionsOverload_ReturnsSameFactory()
    {
        var expected = Substitute.For<ILoggerFactory>();
        var actual = expected.AddSentry(o => o.Dsn = Sentry.SentryConstants.DisableSdkDsnValue);

        Assert.Same(expected, actual);
    }

    [Fact]
    public void AddSentry_ConfigureOptionsOverload_InvokesCallback()
    {
        var expected = Substitute.For<ILoggerFactory>();

        var invoked = false;
        _ = expected.AddSentry(o =>
        {
            o.Dsn = Sentry.SentryConstants.DisableSdkDsnValue;
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
