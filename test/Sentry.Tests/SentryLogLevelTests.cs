namespace Sentry.Tests;

/// <summary>
/// <see href="https://develop.sentry.dev/sdk/telemetry/logs/"/>
/// </summary>
public class SentryLogLevelTests
{
    [Theory]
    [MemberData(nameof(SeverityTextAndSeverityNumber))]
    public void SeverityTextAndSeverityNumber_WithinRange_MatchesProtocol(int level, string text, int? number)
    {
        var @enum = (SentryLogLevel)level;

        var (severityText, severityNumber) = @enum.ToSeverityTextAndOptionalSeverityNumber();

        Assert.Multiple(
            () => Assert.Equal(text, severityText),
            () => Assert.Equal(number, severityNumber));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(25)]
    public void SeverityTextAndSeverityNumber_OutOfRange_ThrowOutOfRange(int level)
    {
        var @enum = (SentryLogLevel)level;

        var exception = Assert.Throws<ArgumentOutOfRangeException>("level", () => @enum.ToSeverityTextAndOptionalSeverityNumber());
        Assert.StartsWith("Severity must be between 1 (inclusive) and 24 (inclusive).", exception.Message);
        Assert.Equal(level, (int)exception.ActualValue!);
    }

    [Fact]
    public void ThrowOutOfRange_WithinRange_DoesNotThrow()
    {
        var range = Enumerable.Range(1, 24);

        var count = 0;
        foreach (var item in range)
        {
            var level = (SentryLogLevel)item;
            SentryLogLevelExtensions.ThrowIfOutOfRange(level);
            count++;
        }

        Assert.Equal(24, count);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(25)]
    public void ThrowOutOfRange_OutOfRange_Throws(int level)
    {
        var @enum = (SentryLogLevel)level;

        var exception = Assert.Throws<ArgumentOutOfRangeException>("@enum", () => SentryLogLevelExtensions.ThrowIfOutOfRange(@enum));
        Assert.StartsWith("Severity must be between 1 (inclusive) and 24 (inclusive).", exception.Message);
        Assert.Equal(level, (int)exception.ActualValue!);
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
