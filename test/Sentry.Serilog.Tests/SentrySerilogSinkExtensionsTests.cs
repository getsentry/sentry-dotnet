using Serilog.Formatting;

namespace Sentry.Serilog.Tests;

public class SentrySerilogSinkExtensionsTests
{
    private class Fixture
    {
        // Parameter values that are NOT set to the default values in SentrySerilogOptions
        public LogEventLevel MinimumEventLevel { get; } = LogEventLevel.Verbose;
        public LogEventLevel MinimumBreadcrumbLevel { get; } = LogEventLevel.Fatal;
        public IFormatProvider FormatProvider { get; } = CultureInfo.InvariantCulture;
        public ITextFormatter TextFormatter { get; } = new MessageTemplateTextFormatter("[{MyTaskId}] {Message}");
        public LogEventLevel RestrictedToMinimumLevel { get; } = LogEventLevel.Warning;
        public LoggingLevelSwitch LevelSwitch { get; } = new(LogEventLevel.Error);

        public static SentrySerilogOptions GetSut() => new();
    }

    private readonly Fixture _fixture = new();

    [Fact]
    public void ConfigureSentrySerilogOptions_NoParameters_LeavesDefaults()
    {
        var sut = Fixture.GetSut();

        SentrySinkExtensions.ConfigureSentrySerilogOptions(sut);

        AssertEqualDeep(new SentrySerilogOptions(), sut);
    }

    [Fact]
    public void ConfigureSentrySerilogOptions_WithMultipleParameters_MakesAppropriateChangesToObject()
    {
        var sut = Fixture.GetSut();

        SentrySinkExtensions.ConfigureSentrySerilogOptions(sut, minimumEventLevel: _fixture.MinimumEventLevel,
            minimumBreadcrumbLevel: _fixture.MinimumBreadcrumbLevel);

        var expected = new SentrySerilogOptions
        {
            MinimumEventLevel = _fixture.MinimumEventLevel,
            MinimumBreadcrumbLevel = _fixture.MinimumBreadcrumbLevel
        };
        AssertEqualDeep(expected, sut);
    }

    [Fact]
    public void ConfigureSentrySerilogOptions_WithAllParameters_MakesAppropriateChangesToObject()
    {
        var sut = Fixture.GetSut();

        SentrySinkExtensions.ConfigureSentrySerilogOptions(sut, _fixture.MinimumEventLevel,
            _fixture.MinimumBreadcrumbLevel, _fixture.FormatProvider, _fixture.TextFormatter,
            _fixture.RestrictedToMinimumLevel, _fixture.LevelSwitch);

        Assert.Equal(_fixture.MinimumEventLevel, sut.MinimumEventLevel);
        Assert.Equal(_fixture.MinimumBreadcrumbLevel, sut.MinimumBreadcrumbLevel);
        Assert.Same(_fixture.FormatProvider, sut.FormatProvider);
        Assert.Same(_fixture.TextFormatter, sut.TextFormatter);
        Assert.Equal(_fixture.RestrictedToMinimumLevel, sut.RestrictedToMinimumLevel);
        Assert.Same(_fixture.LevelSwitch, sut.LevelSwitch);
    }

    [Fact]
    public void Sentry_WithRestrictedToMinimumLevel_ConfigureOptions_FiltersLogsBelow()
    {
        // Arrange
        var hub = Substitute.For<IHub>();
        hub.IsEnabled.Returns(true);
        var options = new SentrySerilogOptions
        {
            MinimumBreadcrumbLevel = LogEventLevel.Verbose,
            MinimumEventLevel = LogEventLevel.Verbose,
            RestrictedToMinimumLevel = LogEventLevel.Error,
        };
        var sink = new SentrySink(options, () => hub, new MockClock());
        using var logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.Sink(sink, options.RestrictedToMinimumLevel, options.LevelSwitch)
            .CreateLogger();

        // Act
        logger.Warning("Below threshold");
        logger.Error("At threshold");

        // Assert: Warning is filtered by Serilog before reaching the sink; only Error gets through
        hub.Received(1).CaptureEvent(Arg.Any<SentryEvent>());
        hub.DidNotReceive().CaptureEvent(Arg.Is<SentryEvent>(e =>
            e.Message.Message == "Below threshold"));
    }

    [Fact]
    public void Sentry_WithRestrictedToMinimumLevel_ParameterIsAccepted()
    {
        var ex = Record.Exception(() =>
            new LoggerConfiguration()
                .WriteTo.Sentry(
                    minimumBreadcrumbLevel: LogEventLevel.Verbose,
                    minimumEventLevel: LogEventLevel.Error,
                    restrictedToMinimumLevel: LogEventLevel.Warning)
                .CreateLogger());

        Assert.Null(ex);
    }

    [Fact]
    public void Sentry_DsnOverload_InvokedByName_Throws()
    {
        var method = typeof(SentrySinkExtensions)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(m => m.Name == nameof(SentrySinkExtensions.Sentry)
                         && m.GetParameters().Any(p => p.Name == "dsn"));

        var arguments = method.GetParameters()
            .Select(p => p.Name == "dsn"
                ? "https://key@sentry.io/1"
                : p.HasDefaultValue ? p.DefaultValue : null)
            .ToArray();
        arguments[0] = new LoggerConfiguration().WriteTo;

        var exception = Assert.Throws<TargetInvocationException>(() => method.Invoke(null, arguments));

        Assert.IsType<NotSupportedException>(exception.InnerException);
        Assert.Contains("no longer initializes the SDK", exception.InnerException!.Message);
    }

    [Fact]
    public void Sentry_DsnOverload_IsObsoleteAsError()
    {
        var method = typeof(SentrySinkExtensions)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(m => m.Name == nameof(SentrySinkExtensions.Sentry)
                         && m.GetParameters().Any(p => p.Name == "dsn"));

        var obsolete = method.GetCustomAttribute<ObsoleteAttribute>();

        Assert.NotNull(obsolete);
        Assert.True(obsolete!.IsError);
    }

    private static void AssertEqualDeep(object expected, object actual)
    {
        var serializedLeftObject = JsonSerializer.Serialize(expected);
        var serializedRightObject = JsonSerializer.Serialize(actual);
        Assert.Equal(serializedLeftObject, serializedRightObject);
    }
}
