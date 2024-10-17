namespace Sentry.Tests;

[UsesVerify]
public partial class HubTests
{
    [SkippableFact]
    public async Task CaptureEvent_ActiveTransaction_UnhandledExceptionTransactionEndedAsCrashed()
    {
        // Flaky on Mono. See https://github.com/getsentry/sentry-dotnet/issues/2751#issuecomment-1863385480
        Skip.If(RuntimeInformation.FrameworkDescription.StartsWith("Mono", StringComparison.OrdinalIgnoreCase));

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
