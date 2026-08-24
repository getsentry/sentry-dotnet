namespace Sentry.Tests;

/// <summary>
/// <see href="https://develop.sentry.dev/sdk/telemetry/metrics/"/>
/// </summary>
public class SentryMetricTypeTests
{
    private readonly InMemoryDiagnosticLogger _logger;

    public SentryMetricTypeTests()
    {
        _logger = new InMemoryDiagnosticLogger();
    }

    [Theory]
    [InlineData(SentryMetricType.Counter, "counter")]
    [InlineData(SentryMetricType.Gauge, "gauge")]
    [InlineData(SentryMetricType.Distribution, "distribution")]
    public void Protocol_WithinRange_Valid(SentryMetricType type, string expected)
    {
#if NET5_0_OR_GREATER
        Assert.True(Enum.IsDefined(type));
#else
        Assert.True(Enum.IsDefined(typeof(SentryMetricType), type));
#endif

        var actual = type.ToProtocolString(_logger);

        Assert.Equal(expected, actual);
        Assert.Empty(_logger.Entries);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(3)]
    public void Protocol_OutOfRange_Invalid(int value)
    {
        var type = (SentryMetricType)value;
#if NET5_0_OR_GREATER
        Assert.False(Enum.IsDefined(type));
#else
        Assert.False(Enum.IsDefined(typeof(SentryMetricType), type));
#endif

        var actual = type.ToProtocolString(_logger);

        Assert.Equal("unknown", actual);
        var entry = Assert.Single(_logger.Entries);
        Assert.Multiple(
            () => Assert.Equal(SentryLevel.Debug, entry.Level),
            () => Assert.Equal("Metric type {0} is not defined.", entry.Message),
            () => Assert.Null(entry.Exception),
            () => Assert.Equal([type], entry.Args));
    }
}
