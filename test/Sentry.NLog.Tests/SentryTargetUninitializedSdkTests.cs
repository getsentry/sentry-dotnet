namespace Sentry.NLog.Tests;

public class SentryTargetUninitializedSdkTests
{
    private readonly List<string> _internalLog = [];
    private readonly List<string> _standardError = [];

    private Logger GetLogger(bool sdkEnabled, string dsn = ValidDsn)
    {
        var hub = Substitute.For<IHub>();
        hub.IsEnabled.Returns(sdkEnabled);
        hub.Logger.Returns(new InMemorySentryStructuredLogger());

        var warning = new UninitializedSdkWarning(_internalLog.Add)
        {
            DsnLocator = () => dsn,
            WriteToStandardError = _standardError.Add
        };

        var target = new SentryTarget(new SentryNLogOptions(), () => hub, new MockClock(), warning)
        {
            Name = "sentry"
        };

        var factory = new LogFactory();
        var configuration = new LoggingConfiguration(factory);
        configuration.AddTarget("sentry", target);
        configuration.AddRule(LogLevel.Trace, LogLevel.Fatal, target);
        factory.Configuration = configuration;

        return factory.GetLogger("sentry");
    }

    [Fact]
    public void Write_SdkNotInitialized_WarnsToInternalLoggerAndStandardError()
    {
        GetLogger(sdkEnabled: false).Error("message");

        Assert.Equal(new[] { SentryTarget.UninitializedSdkMessage }, _internalLog);
        Assert.Equal(new[] { SentryTarget.UninitializedSdkMessage }, _standardError);
    }

    [Fact]
    public void Write_SdkNotInitialized_WarnsOnceForManyEvents()
    {
        var logger = GetLogger(sdkEnabled: false);

        logger.Error("first");
        logger.Fatal("second");

        Assert.Single(_standardError);
    }

    [Fact]
    public void Write_SdkNotInitializedAndBelowMinimumEventLevel_DoesNotWarn()
    {
        GetLogger(sdkEnabled: false).Warn("message");

        Assert.Empty(_internalLog);
        Assert.Empty(_standardError);
    }

    [Fact]
    public void Write_SdkNotInitializedAndNoDsnFound_DoesNotWarn()
    {
        GetLogger(sdkEnabled: false, dsn: null).Error("message");

        Assert.Empty(_internalLog);
        Assert.Empty(_standardError);
    }

    [Fact]
    public void Write_SdkInitialized_DoesNotWarn()
    {
        GetLogger(sdkEnabled: true).Error("message");

        Assert.Empty(_internalLog);
        Assert.Empty(_standardError);
    }
}
