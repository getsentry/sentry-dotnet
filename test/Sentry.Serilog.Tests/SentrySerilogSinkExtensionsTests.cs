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
    public void ConfigureSentrySerilogOptions_WithNoParameters_MakesNoChangesToObject()
    {
        var sut = Fixture.GetSut();

        // Make the call with only the required parameter
        SentrySinkExtensions.ConfigureSentrySerilogOptions(sut);

        // Compare. I'm not sure how to deep compare--I don't see a nuget ref to that type
        // of functionality and I'm hesitant to introduce new technologies with such a
        // small commit.
        AssertEqualDeep(_fixture.Options, sut);
    }

    [Fact]
    public void ConfigureSentrySerilogOptions_WithOneParameter_MakesAppropriateChangeToObject()
    {
        var sut = Fixture.GetSut();

        // Make the call with only the required parameter
        const bool sendDefaultPii = true;
        SentrySinkExtensions.ConfigureSentrySerilogOptions(sut, sendDefaultPii: sendDefaultPii);

        // Compare. I'm not sure how to deep compare--I don't see a nuget ref to that type
        // of functionality and I'm hesitant to introduce new technologies with such a
        // small commit.
        AssertNotEqualDeep(_fixture.Options, sut);

        Assert.Equal(sendDefaultPii, sut.SendDefaultPii);
    }

    [Fact]
    public void ConfigureSentrySerilogOptions_WithMultipleParameters_MakesAppropriateChangesToObject()
    {
        var sut = Fixture.GetSut();

        SentrySinkExtensions.ConfigureSentrySerilogOptions(sut, sendDefaultPii: _fixture.SendDefaultPii,
            decompressionMethods: _fixture.DecompressionMethods, reportAssembliesMode: _fixture.ReportAssembliesMode, sampleRate: _fixture.SampleRate);

        // Fail early
        AssertNotEqualDeep(_fixture.Options, sut);

        // Compare individual properties
        Assert.Equal(_fixture.SendDefaultPii, sut.SendDefaultPii);
        Assert.Equal(_fixture.DecompressionMethods, sut.DecompressionMethods);
        Assert.Equal(_fixture.ReportAssembliesMode, sut.ReportAssembliesMode);
        Assert.Equal(_fixture.SampleRate, sut.SampleRate);
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
            _fixture.ReportAssembliesMode, _fixture.DeduplicateMode, _fixture.InitializeSdk);

        // Fail early
        AssertNotEqualDeep(_fixture.Options, sut);

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
        Assert.Equal(_fixture.InitializeSdk, sut.InitializeSdk);
        Assert.Equal(_fixture.MinimumEventLevel, sut.MinimumEventLevel);
        Assert.Equal(_fixture.MinimumBreadcrumbLevel, sut.MinimumBreadcrumbLevel);
    }

    private static void AssertEqualDeep(object expected, object actual)
    {
        AssertDeep(expected, actual, true);
    }

    private static void AssertNotEqualDeep(object left, object right)
    {
        AssertDeep(left, right, false);
    }

    private static void AssertDeep(object left, object right, bool shouldCheckEqual)
    {
        var serializedLeftObject = JsonSerializer.Serialize(left);
        var serializedRightObject = JsonSerializer.Serialize(right);

        if (shouldCheckEqual)
        {
            Assert.Equal(serializedLeftObject, serializedRightObject);
        }
        else
        {
            Assert.NotEqual(serializedLeftObject, serializedRightObject);
        }
    }
}
