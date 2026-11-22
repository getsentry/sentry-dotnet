namespace Sentry.Log4Net.Tests;

public class SentryAppenderUninitializedSdkTests : IDisposable
{
    private readonly List<string> _internalLog = [];
    private readonly List<string> _standardError = [];

    public void Dispose() => SentryClientExtensions.SentryOptionsForTestingOnly = null;

    private SentryAppender GetSut(bool sdkEnabled, string dsn = ValidDsn, Level minimumEventLevel = null)
    {
        var hub = Substitute.For<IHub>();
        hub.IsEnabled.Returns(sdkEnabled);
        hub.Logger.Returns(new InMemorySentryStructuredLogger());

        var warning = new UninitializedSdkWarning(_internalLog.Add)
        {
            DsnLocator = () => dsn,
            WriteToStandardError = _standardError.Add
        };

        var sut = new SentryAppender(hub, warning) { MinimumEventLevel = minimumEventLevel };
        sut.ActivateOptions();
        return sut;
    }

    private static LoggingEvent LoggingEventAt(Level level) =>
        new(null, null, "logger", level, "message", null);

    [Fact]
    public void Append_SdkNotInitialized_WarnsToInternalLogAndStandardError()
    {
        GetSut(sdkEnabled: false).DoAppend(LoggingEventAt(Level.Error));

        Assert.Equal(new[] { SentryAppender.UninitializedSdkMessage }, _internalLog);
        Assert.Equal(new[] { SentryAppender.UninitializedSdkMessage }, _standardError);
    }

    [Fact]
    public void Append_SdkNotInitialized_WarnsOnceForManyEvents()
    {
        var sut = GetSut(sdkEnabled: false);

        sut.DoAppend(LoggingEventAt(Level.Error));
        sut.DoAppend(LoggingEventAt(Level.Critical));

        Assert.Single(_standardError);
    }

    [Fact]
    public void Append_SdkNotInitializedAndBelowMinimumEventLevel_DoesNotWarn()
    {
        var sut = GetSut(sdkEnabled: false, minimumEventLevel: Level.Error);

        sut.DoAppend(LoggingEventAt(Level.Warn));

        Assert.Empty(_internalLog);
        Assert.Empty(_standardError);
    }

    [Fact]
    public void Append_SdkNotInitializedAndNoDsnFound_DoesNotWarn()
    {
        GetSut(sdkEnabled: false, dsn: null).DoAppend(LoggingEventAt(Level.Error));

        Assert.Empty(_internalLog);
        Assert.Empty(_standardError);
    }

    [Fact]
    public void Append_SdkInitialized_DoesNotWarn()
    {
        GetSut(sdkEnabled: true).DoAppend(LoggingEventAt(Level.Error));

        Assert.Empty(_internalLog);
        Assert.Empty(_standardError);
    }
}
