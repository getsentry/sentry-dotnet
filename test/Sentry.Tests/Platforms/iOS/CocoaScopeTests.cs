#if IOS
using Sentry.Cocoa;

namespace Sentry.Tests.Platforms.iOS;

public class CocoaScopeTests
{
    [Fact]
    public void Configure_DuringNativeCall_IsConfiguring()
    {
        var isConfiguring = false;

        CocoaScope.Configure(_ => { }, configureNativeScope: _ => isConfiguring = CocoaScope.IsConfiguring);

        Assert.True(isConfiguring);
        Assert.False(CocoaScope.IsConfiguring);
    }

    [Fact]
    public void Configure_NativeCallThrows_LogsError()
    {
        var logger = new InMemoryDiagnosticLogger();
        var options = new SentryOptions { Debug = true, DiagnosticLogger = logger };
        var exception = new InvalidOperationException();

        CocoaScope.Configure(_ => { }, options, _ => throw exception);

        Assert.Contains(logger.Entries, entry => entry.Level == SentryLevel.Error && entry.Exception == exception);
        Assert.False(CocoaScope.IsConfiguring);
    }
}
#endif
