using Sentry.PlatformAbstractions;

namespace Sentry.Tests;

[UsesVerify]
public partial class HubTests
{
    [SkippableFact]
    public async Task CaptureEvent_ActiveTransaction_UnhandledExceptionTransactionEndedAsCrashed()
    {
        // See https://github.com/getsentry/sentry-dotnet/issues/2785
        Skip.If(RuntimeInfo.GetRuntime().IsMono() && TestEnvironment.IsGitHubActions);

        // Arrange
        var worker = new FakeBackgroundWorker();

        var options = new SentryOptions
        {
            Dsn = ValidDsn,
            Release = "release",
            TracesSampleRate = 1.0
        };
        var sessionManager = new GlobalSessionManager(options);
        var client = new SentryClient(options, worker, sessionManager: sessionManager);
        var hub = new Hub(options, client, sessionManager);

        var transaction = hub.StartTransaction("my transaction", "my operation");
        hub.ConfigureScope(scope => scope.Transaction = transaction);
        hub.StartSession();

        // Act
        hub.CaptureEvent(new()
        {
            SentryExceptions = new[]
            {
                new SentryException
                {
                    Mechanism = new()
                    {
                        Handled = false
                    }
                }
            }
        });

        await Verify(worker.Envelopes)
            .UniqueForRuntimeAndVersion()
            .IgnoreStandardSentryMembers()
            .IgnoreMember("Stacktrace")
            .IgnoreMember<SentryThread>(_ => _.Name)
            .IgnoreInstance<DebugImage>(_ =>
                _.DebugFile != null && (
                    _.DebugFile.Contains("Xunit.SkippableFact") ||
                    _.DebugFile.Contains("xunit.runner") ||
                    _.DebugFile.Contains("JetBrains.ReSharper.TestRunner") ||
                    _.DebugFile.Contains("Microsoft.TestPlatform") ||
                    _.DebugFile.Contains("Microsoft.VisualStudio.TestPlatform.Common.pdb")
                )
            );
    }
}
