using Sentry.PlatformAbstractions;

namespace Sentry.Tests.PlatformAbstractions;

public class SentryRuntimeTests
{
    [Fact]
    public void Current_Equal()
    {
        Assert.Equal(SentryRuntime.Current, SentryRuntime.Current);
    }
}
