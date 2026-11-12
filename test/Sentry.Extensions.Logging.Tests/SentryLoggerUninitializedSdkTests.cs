using Microsoft.Extensions.Logging;

namespace Sentry.Extensions.Logging.Tests;

public class SentryLoggerUninitializedSdkTests
{
    private readonly List<string> _standardError = [];

    private SentryLogger GetSut(bool sdkEnabled, string dsn = ValidDsn, SentryLoggingOptions options = null)
    {
        var hub = Substitute.For<IHub>();
        hub.IsEnabled.Returns(sdkEnabled);
        hub.Logger.Returns(new InMemorySentryStructuredLogger());

        var warning = new UninitializedSdkWarning
        {
            DsnLocator = () => dsn,
            WriteToStandardError = _standardError.Add
        };

        return new SentryLogger("SomeApp", options ?? new SentryLoggingOptions(), new MockClock(), hub, warning);
    }

    [Fact]
    public void Log_SdkNotInitialized_WarnsToStandardError()
    {
        GetSut(sdkEnabled: false).LogError("message");

        Assert.Equal(new[] { SentryLogger.UninitializedSdkMessage }, _standardError);
    }

    [Fact]
    public void Log_SdkNotInitialized_WarnsOnceForManyEvents()
    {
        var sut = GetSut(sdkEnabled: false);

        sut.LogError("first");
        sut.LogCritical("second");

        Assert.Single(_standardError);
    }

    [Fact]
    public void Log_SdkNotInitializedAndBelowMinimumEventLevel_DoesNotWarn()
    {
        GetSut(sdkEnabled: false).LogWarning("message");

        Assert.Empty(_standardError);
    }

    [Fact]
    public void Log_SdkNotInitializedAndEventsDisabled_DoesNotWarn()
    {
        var options = new SentryLoggingOptions { MinimumEventLevel = LogLevel.None };

        GetSut(sdkEnabled: false, options: options).LogCritical("message");

        Assert.Empty(_standardError);
    }

    [Fact]
    public void Log_SdkNotInitializedAndNoDsnFound_DoesNotWarn()
    {
        GetSut(sdkEnabled: false, dsn: null).LogError("message");

        Assert.Empty(_standardError);
    }

    [Fact]
    public void Log_SdkInitialized_DoesNotWarn()
    {
        GetSut(sdkEnabled: true).LogError("message");

        Assert.Empty(_standardError);
    }

    [Fact]
    public void CreateLogger_SdkNotInitialized_WarnsOnceAcrossCategories()
    {
        var hub = Substitute.For<IHub>();
        hub.IsEnabled.Returns(false);
        var warning = new UninitializedSdkWarning
        {
            DsnLocator = () => ValidDsn,
            WriteToStandardError = _standardError.Add
        };
        var provider = new SentryLoggerProvider(hub, new MockClock(), new SentryLoggingOptions(), warning);

        provider.CreateLogger("FirstCategory").LogError("first");
        provider.CreateLogger("SecondCategory").LogError("second");

        Assert.Single(_standardError);
    }
}
