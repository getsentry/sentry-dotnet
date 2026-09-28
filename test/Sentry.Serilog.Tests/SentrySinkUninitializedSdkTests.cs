namespace Sentry.Serilog.Tests;

public class SentrySinkUninitializedSdkTests
{
    private readonly List<string> _selfLog = [];
    private readonly List<string> _standardError = [];

    private SentrySink GetSut(bool sdkEnabled, string dsn = ValidDsn)
    {
        var hub = Substitute.For<IHub>();
        hub.IsEnabled.Returns(sdkEnabled);
        hub.Logger.Returns(new InMemorySentryStructuredLogger());

        var warning = new UninitializedSdkWarning(_selfLog.Add)
        {
            DsnLocator = () => dsn,
            WriteToStandardError = _standardError.Add
        };

        return new SentrySink(new SentrySerilogOptions(), () => hub, new MockClock(), warning);
    }

    private static LogEvent LogEventAt(LogEventLevel level) =>
        new(DateTimeOffset.UtcNow, level, null, MessageTemplate.Empty, []);

    [Fact]
    public void Emit_SdkNotInitialized_WarnsToSelfLogAndStandardError()
    {
        var sut = GetSut(sdkEnabled: false);

        sut.Emit(LogEventAt(LogEventLevel.Error));

        Assert.Equal(new[] { SentrySink.UninitializedSdkMessage }, _selfLog);
        Assert.Equal(new[] { SentrySink.UninitializedSdkMessage }, _standardError);
    }

    [Fact]
    public void Emit_SdkNotInitialized_WarnsOnceForManyEvents()
    {
        var sut = GetSut(sdkEnabled: false);

        sut.Emit(LogEventAt(LogEventLevel.Error));
        sut.Emit(LogEventAt(LogEventLevel.Fatal));

        Assert.Single(_standardError);
    }

    [Fact]
    public void Emit_SdkNotInitializedAndBelowMinimumEventLevel_DoesNotWarn()
    {
        var sut = GetSut(sdkEnabled: false);

        sut.Emit(LogEventAt(LogEventLevel.Warning));

        Assert.Empty(_selfLog);
        Assert.Empty(_standardError);
    }

    [Fact]
    public void Emit_SdkNotInitializedAndNoDsnFound_DoesNotWarn()
    {
        var sut = GetSut(sdkEnabled: false, dsn: null);

        sut.Emit(LogEventAt(LogEventLevel.Error));

        Assert.Empty(_selfLog);
        Assert.Empty(_standardError);
    }

    [Fact]
    public void Emit_SdkInitialized_DoesNotWarn()
    {
        var sut = GetSut(sdkEnabled: true);

        sut.Emit(LogEventAt(LogEventLevel.Error));

        Assert.Empty(_selfLog);
        Assert.Empty(_standardError);
    }
}
