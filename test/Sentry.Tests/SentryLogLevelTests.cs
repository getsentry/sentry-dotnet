namespace Sentry.Tests;

/// <summary>
/// <see href="https://develop.sentry.dev/sdk/telemetry/logs/"/>
/// </summary>
public class SentryLogLevelTests
{
    private readonly InMemoryDiagnosticLogger _logger;

    public SentryLogLevelTests()
    {
        _logger = new InMemoryDiagnosticLogger();
    }

#if NET7_0_OR_GREATER
    [Fact]
    public void Enum_GetValuesAsUnderlyingType_LowestSeverityNumberPerSeverityRange()
    {
        var values = Enum.GetValuesAsUnderlyingType<SentryLogLevel>();

        Assert.Collection(values.OfType<int>(),
            element => Assert.Equal(1, element),
            element => Assert.Equal(5, element),
            element => Assert.Equal(9, element),
            element => Assert.Equal(13, element),
            element => Assert.Equal(17, element),
            element => Assert.Equal(21, element));
    }
#endif

    [Theory]
    [MemberData(nameof(SeverityTextAndSeverityNumber))]
    public void SeverityTextAndSeverityNumber_WithinRange_MatchesProtocol(int level, string text, int? number)
    {
        var @enum = (SentryLogLevel)level;

        var (severityText, severityNumber) = @enum.ToSeverityTextAndOptionalSeverityNumber(_logger);

        Assert.Multiple(
            () => Assert.Equal(text, severityText),
            () => Assert.Equal(number, severityNumber));
        Assert.Empty(_logger.Entries);
    }

    [Theory]
    [InlineData(0, "trace", 1, "minimum")]
    [InlineData(25, "fatal", 24, "maximum")]
    public void SeverityTextAndSeverityNumber_OutOfRange_ClampValue(int level, string text, int? number, string clamp)
    {
        var @enum = (SentryLogLevel)level;

        var (severityText, severityNumber) = @enum.ToSeverityTextAndOptionalSeverityNumber(_logger);

        Assert.Multiple(
            () => Assert.Equal(text, severityText),
            () => Assert.Equal(number, severityNumber));
        var entry = Assert.Single(_logger.Entries);
        Assert.Multiple(
            () => Assert.Equal(SentryLevel.Debug, entry.Level),
            () => Assert.Equal($$"""Log level {0} out of range ... clamping to {{clamp}} value {1} ({2})""", entry.Message),
            () => Assert.Null(entry.Exception),
            () => Assert.Equal([@enum, number, text], entry.Args));
    }

    public static TheoryData<int, string, int?> SeverityTextAndSeverityNumber()
    {
        return new TheoryData<int, string, int?>
        {
            { 1, "trace", null },
            { 2, "trace", 2 },
            { 3, "trace", 3 },
            { 4, "trace", 4 },
            { 5, "debug", null },
            { 6, "debug", 6 },
            { 7, "debug", 7 },
            { 8, "debug", 8 },
            { 9, "info", null },
            { 10, "info", 10 },
            { 11, "info", 11 },
            { 12, "info", 12 },
            { 13, "warn", null },
            { 14, "warn", 14 },
            { 15, "warn", 15 },
            { 16, "warn", 16 },
            { 17, "error", null },
            { 18, "error", 18 },
            { 19, "error", 19 },
            { 20, "error", 20 },
            { 21, "fatal", null },
            { 22, "fatal", 22 },
            { 23, "fatal", 23 },
            { 24, "fatal", 24 },
        };
    }

    [Theory]
    [MemberData(nameof(Create))]
    public void Create_WithinRange_UsesLowestSeverityNumberOfRange(int value, SentryLogLevel level)
    {
        var @enum = SentryLogLevelExtensions.FromValue(value, _logger);

        Assert.Equal(level, @enum);
        Assert.Empty(_logger.Entries);
    }

    [Theory]
    [InlineData(0, SentryLogLevel.Trace, "minimum")]
    [InlineData(25, SentryLogLevel.Fatal, "maximum")]
    public void Create_OutOfRange_ClampValue(int value, SentryLogLevel level, string clamp)
    {
        var @enum = SentryLogLevelExtensions.FromValue(value, _logger);

        Assert.Equal(level, @enum);
        var entry = Assert.Single(_logger.Entries);
        Assert.Multiple(
            () => Assert.Equal(SentryLevel.Debug, entry.Level),
            () => Assert.Equal($$"""Log number {0} out of range ... clamping to {{clamp}} level {1}""", entry.Message),
            () => Assert.Null(entry.Exception),
            () => Assert.Equal([value, level], entry.Args));
    }

    public static TheoryData<int, SentryLogLevel> Create()
    {
        return new TheoryData<int, SentryLogLevel>
        {
            { 1, SentryLogLevel.Trace },
            { 2, SentryLogLevel.Trace },
            { 3, SentryLogLevel.Trace },
            { 4, SentryLogLevel.Trace },
            { 5, SentryLogLevel.Debug },
            { 6, SentryLogLevel.Debug },
            { 7, SentryLogLevel.Debug },
            { 8, SentryLogLevel.Debug },
            { 9, SentryLogLevel.Info },
            { 10, SentryLogLevel.Info },
            { 11, SentryLogLevel.Info },
            { 12, SentryLogLevel.Info },
            { 13, SentryLogLevel.Warning },
            { 14, SentryLogLevel.Warning },
            { 15, SentryLogLevel.Warning },
            { 16, SentryLogLevel.Warning },
            { 17, SentryLogLevel.Error },
            { 18, SentryLogLevel.Error },
            { 19, SentryLogLevel.Error },
            { 20, SentryLogLevel.Error },
            { 21, SentryLogLevel.Fatal },
            { 22, SentryLogLevel.Fatal },
            { 23, SentryLogLevel.Fatal },
            { 24, SentryLogLevel.Fatal },
        };
    }

    [Theory]
    [MemberData(nameof(Convert))]
    public void Convert_FromSentryLogLevel_ToSentryLevel(int level, SentryLevel expected)
    {
        var @enum = (SentryLogLevel)level;

        var actual = @enum.ToSentryLevel();

        Assert.Equal(expected, actual);
    }

    public static TheoryData<int, SentryLevel> Convert()
    {
        return new TheoryData<int, SentryLevel>
        {
            { 0, SentryLevel.Debug },
            { 1, SentryLevel.Debug },
            { 2, SentryLevel.Debug },
            { 3, SentryLevel.Debug },
            { 4, SentryLevel.Debug },
            { 5, SentryLevel.Debug },
            { 6, SentryLevel.Debug },
            { 7, SentryLevel.Debug },
            { 8, SentryLevel.Debug },
            { 9, SentryLevel.Info },
            { 10, SentryLevel.Info },
            { 11, SentryLevel.Info },
            { 12, SentryLevel.Info },
            { 13, SentryLevel.Warning },
            { 14, SentryLevel.Warning },
            { 15, SentryLevel.Warning },
            { 16, SentryLevel.Warning },
            { 17, SentryLevel.Error },
            { 18, SentryLevel.Error },
            { 19, SentryLevel.Error },
            { 20, SentryLevel.Error },
            { 21, SentryLevel.Fatal },
            { 22, SentryLevel.Fatal },
            { 23, SentryLevel.Fatal },
            { 24, SentryLevel.Fatal },
            { 25, SentryLevel.Fatal },
        };
    }
}
