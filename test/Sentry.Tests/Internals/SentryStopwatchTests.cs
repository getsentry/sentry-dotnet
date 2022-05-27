namespace Sentry.Tests.Internals;

public class SentryStopwatchTests
{
    private static readonly TimeSpan TestPrecision = TimeSpan.FromMilliseconds(10);

    [Fact]
    public void StartDateTimeOffset_IsValid()
    {
        var sw = SentryStopwatch.StartNew();
        var start = sw.StartDateTimeOffset;

        start.Should().BeCloseTo(DateTimeOffset.UtcNow, TestPrecision);
    }


    [Fact]
    public void CurrentDateTimeOffset_IsValid()
    {
        var sw = SentryStopwatch.StartNew();
        Thread.Sleep(TimeSpan.FromMilliseconds(100));
        var current = sw.CurrentDateTimeOffset;

        current.Should().BeCloseTo(DateTimeOffset.UtcNow, TestPrecision);
    }

    [Fact]
    public void Elapsed_IsValid()
    {
        var sleepTime = TimeSpan.FromMilliseconds(100);

        var sw = SentryStopwatch.StartNew();
        Thread.Sleep(sleepTime);
        var elapsed = sw.Elapsed;

        elapsed.Should().BeCloseTo(sleepTime, TestPrecision);
    }
}
