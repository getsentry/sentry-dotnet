namespace Sentry.Serilog.Tests;

public class SentrySerilogSinkExtensionsTests
{
    private class Fixture
    {
        public SentrySerilogOptions Options { get; } = new();

        // Parameter values that are NOT set to the default values in SentryOptions or SentrySerilogOptions
        public bool SendDefaultPii { get; } = true;
        public bool IsEnvironmentUser { get; } = false;
        public string ServerName { get; } = nameof(ConfigureSentrySerilogOptions_WithAllParameters_MakesAppropriateChangesToObject);
        public bool AttachStackTrace { get; } = true;
        public int MaxBreadcrumbs { get; } = 9;
        public float SampleRate { get; } = 0.4f;
        public string Release { get; } = nameof(ConfigureSentrySerilogOptions_WithAllParameters_MakesAppropriateChangesToObject);
        public string Environment { get; } = nameof(ConfigureSentrySerilogOptions_WithAllParameters_MakesAppropriateChangesToObject);
        public string Dsn { get; } = ValidDsn;
        public int MaxQueueItems { get; } = 17;
        public TimeSpan ShutdownTimeout { get; } = TimeSpan.FromDays(1.3);
        public DecompressionMethods DecompressionMethods { get; } = DecompressionMethods.Deflate & DecompressionMethods.GZip;
        public CompressionLevel RequestBodyCompressionLevel { get; } = CompressionLevel.NoCompression;
        public bool RequestBodyCompressionBuffered { get; } = false;
        public bool Debug { get; } = true;
        public SentryLevel DiagnosticLevel { get; } = SentryLevel.Warning;
        public ReportAssembliesMode ReportAssembliesMode { get; } = ReportAssembliesMode.None;
        public DeduplicateMode DeduplicateMode { get; } = DeduplicateMode.SameExceptionInstance;
        public bool InitializeSdk { get; } = false;
        public LogEventLevel MinimumEventLevel { get; } = LogEventLevel.Verbose;
        public LogEventLevel MinimumBreadcrumbLevel { get; } = LogEventLevel.Fatal;

        public static SentrySerilogOptions GetSut() => new();
    }

    private readonly Fixture _fixture = new();

    [Fact]
    public void ConfigureSentrySerilogOptions_WithDsn_InitializeSdk()
    {
        var sut = Fixture.GetSut();

        // Make the call with only the required parameter
        SentrySinkExtensions.ConfigureSentrySerilogOptions(sut, _fixture.Dsn);

        // Compare. I'm not sure how to deep compare--I don't see a nuget ref to that type
        // of functionality and I'm hesitant to introduce new technologies with such a
        // small commit.
        _fixture.Options.Dsn = _fixture.Dsn;
        AssertEqualDeep(_fixture.Options, sut);
        Assert.True(sut.InitializeSdk);
    }

    [Fact]
    public void ConfigureSentrySerilogOptions_NoDsn_DontInitializeSdk()
    {
        var sut = Fixture.GetSut();

        // Make the call with only the required parameter
        SentrySinkExtensions.ConfigureSentrySerilogOptions(sut, null, minimumEventLevel: _fixture.MinimumEventLevel,
            minimumBreadcrumbLevel: _fixture.MinimumBreadcrumbLevel);

        // Compare. I'm not sure how to deep compare--I don't see a nuget ref to that type
        // of functionality and I'm hesitant to introduce new technologies with such a
        // small commit.
        _fixture.Options.MinimumEventLevel = _fixture.MinimumEventLevel;
        _fixture.Options.MinimumBreadcrumbLevel = _fixture.MinimumBreadcrumbLevel;
        AssertEqualDeep(_fixture.Options, sut);
        Assert.False(sut.InitializeSdk);
    }

    [Fact]
    public void ConfigureSentrySerilogOptions_WithMultipleParameters_MakesAppropriateChangesToObject()
    {
        var sut = Fixture.GetSut();

        SentrySinkExtensions.ConfigureSentrySerilogOptions(sut, _fixture.Dsn, sendDefaultPii: _fixture.SendDefaultPii,
            decompressionMethods: _fixture.DecompressionMethods, reportAssembliesMode: _fixture.ReportAssembliesMode, sampleRate: _fixture.SampleRate);

        // Assert
        _fixture.Options.Dsn = _fixture.Dsn;
        _fixture.Options.SendDefaultPii = _fixture.SendDefaultPii;
        _fixture.Options.DecompressionMethods = _fixture.DecompressionMethods;
        _fixture.Options.ReportAssembliesMode = _fixture.ReportAssembliesMode;
        _fixture.Options.SampleRate = _fixture.SampleRate;
        AssertEqualDeep(_fixture.Options, sut);
    }

    [Fact]
    public void ConfigureSentrySerilogOptions_WithAllParameters_MakesAppropriateChangesToObject()
    {
        var sut = Fixture.GetSut();

        SentrySinkExtensions.ConfigureSentrySerilogOptions(sut, _fixture.Dsn, _fixture.MinimumEventLevel,
            _fixture.MinimumBreadcrumbLevel, null, null, _fixture.SendDefaultPii,
            _fixture.IsEnvironmentUser, _fixture.ServerName, _fixture.AttachStackTrace, _fixture.MaxBreadcrumbs,
            _fixture.SampleRate, _fixture.Release, _fixture.Environment, _fixture.MaxQueueItems,
            _fixture.ShutdownTimeout, _fixture.DecompressionMethods, _fixture.RequestBodyCompressionLevel,
            _fixture.RequestBodyCompressionBuffered, _fixture.Debug, _fixture.DiagnosticLevel,
            _fixture.ReportAssembliesMode, _fixture.DeduplicateMode);

        // Compare individual properties
        Assert.Equal(_fixture.SendDefaultPii, sut.SendDefaultPii);
        Assert.Equal(_fixture.IsEnvironmentUser, sut.IsEnvironmentUser);
        Assert.Equal(_fixture.ServerName, sut.ServerName);
        Assert.Equal(_fixture.AttachStackTrace, sut.AttachStacktrace);
        Assert.Equal(_fixture.MaxBreadcrumbs, sut.MaxBreadcrumbs);
        Assert.Equal(_fixture.SampleRate, sut.SampleRate);
        Assert.Equal(_fixture.Release, sut.Release);
        Assert.Equal(_fixture.Environment, sut.Environment);
        Assert.Equal(_fixture.Dsn, sut.Dsn!);
        Assert.Equal(_fixture.MaxQueueItems, sut.MaxQueueItems);
        Assert.Equal(_fixture.ShutdownTimeout, sut.ShutdownTimeout);
        Assert.Equal(_fixture.DecompressionMethods, sut.DecompressionMethods);
        Assert.Equal(_fixture.RequestBodyCompressionLevel, sut.RequestBodyCompressionLevel);
        Assert.Equal(_fixture.RequestBodyCompressionBuffered, sut.RequestBodyCompressionBuffered);
        Assert.Equal(_fixture.Debug, sut.Debug);
        Assert.Equal(_fixture.DiagnosticLevel, sut.DiagnosticLevel);
        Assert.Equal(_fixture.ReportAssembliesMode, sut.ReportAssembliesMode);
        Assert.Equal(_fixture.DeduplicateMode, sut.DeduplicateMode);
        Assert.True(sut.InitializeSdk);
        Assert.Equal(_fixture.MinimumEventLevel, sut.MinimumEventLevel);
        Assert.Equal(_fixture.MinimumBreadcrumbLevel, sut.MinimumBreadcrumbLevel);
    }

    private static void AssertEqualDeep(object expected, object actual)
    {
        var serializedLeftObject = JsonSerializer.Serialize(expected);
        var serializedRightObject = JsonSerializer.Serialize(actual);
        Assert.Equal(serializedLeftObject, serializedRightObject);
    }
}
