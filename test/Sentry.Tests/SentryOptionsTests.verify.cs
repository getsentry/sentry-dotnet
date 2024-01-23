namespace Sentry.Tests;

[UsesVerify]
public partial class SentryOptionsTests
{
    [Fact]
    public Task Integrations_default_ones_are_properly_registered()
    {
        InMemoryDiagnosticLogger logger = new();
        SentryOptions options = new()
        {
            Dsn = ValidDsn,
            Debug = true,
            IsGlobalModeEnabled = true,
            DiagnosticLogger = logger,
            EnableTracing = true,
            TracesSampleRate = 1.0
        };
        Hub _ = new(options, Substitute.For<ISentryClient>());

        var settingsTask = Verify(logger.Entries)
            .UniqueForTargetFrameworkAndVersion()
            .UniqueForRuntime()
            .AutoVerify(includeBuildServer: false);
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            settingsTask = settingsTask.UniqueForOSPlatform();
        return settingsTask;
        // return Verify(logger.Entries)
        //     .UniqueForTargetFrameworkAndVersion()
        //     .UniqueForOSPlatform()
        //     .UniqueForRuntime()
        //     .AutoVerify(includeBuildServer: false);
    }
}
