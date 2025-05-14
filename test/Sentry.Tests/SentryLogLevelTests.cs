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
}
