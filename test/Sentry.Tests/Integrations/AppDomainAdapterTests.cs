#if !NETCOREAPP // Test runner fails when running on netcoreapp
using Sentry.PlatformAbstractions;

namespace Sentry.Tests.Integrations;

public class AppDomainAdapterTests
{
    [SkippableFact]
    public void UnhandledException_FiredOnExceptionUnhandledInThread()
    {
        // Test flaky on Mono
        Skip.If(SentryRuntime.Current.IsMono());

        var evt = new ManualResetEventSlim(false);
        AppDomainAdapter.Instance.UnhandledException += (_, _) => evt.Set();

        var thread = new Thread(() => throw new Exception())
        {
            IsBackground = false
        };

        thread.Start();
        Assert.True(evt.Wait(TimeSpan.FromSeconds(3)));
    }
}

#endif
