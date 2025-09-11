namespace Sentry.Tests.Internals;

public class SentryStopwatchTests
{
    // Note: We can really can't test this at fine precision.  This is just to make sure we're in the right ballpark.
    private static readonly TimeSpan TestPrecision = TimeSpan.FromMilliseconds(500);

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

    [SkippableFact]
    public void Elapsed_IsValid()
    {
#if IOS
        Skip.If(TestEnvironment.IsGitHubActions, "Flaky on iOS in CI.");
#endif

        var sleepTime = TimeSpan.FromMilliseconds(100);

        var sw = SentryStopwatch.StartNew();
        Thread.Sleep(sleepTime);
        var elapsed = sw.Elapsed;

        elapsed.Should().BeGreaterThan(TimeSpan.Zero);
        elapsed.Should().BeCloseTo(sleepTime, TestPrecision);
    }
}
