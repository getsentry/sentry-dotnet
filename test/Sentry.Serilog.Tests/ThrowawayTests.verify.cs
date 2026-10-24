namespace Sentry.Serilog.Tests;

public class ThrowawayTests
{
    [Fact]
    public void AlwaysFails() => Assert.Fail("Throwaway test that checks how a failing background test step surfaces in CI");

    [Fact]
    public Task NewSnapshotWithoutVerifiedFile() => Verify("throwaway");
}
