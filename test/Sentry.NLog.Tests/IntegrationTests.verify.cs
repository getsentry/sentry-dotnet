using Sentry.PlatformAbstractions;

namespace Sentry.NLog.Tests;

public class IntegrationTests
{
    [SkippableFact]
    public Task Simple()
    {
        // See: https://github.com/getsentry/sentry-dotnet/issues/3823
        Skip.If(RuntimeInfo.GetRuntime().IsMono() && TestEnvironment.IsGitHubActions, "Missing DebugImage in CI for Mono");

        var transport = new RecordingTransport();

        var configuration = new LoggingConfiguration();

        configuration.AddSentry(
            options =>
            {
                options.TracesSampleRate = 1;
                options.Layout = "${message}";
                options.Transport = transport;
                options.DiagnosticLevel = SentryLevel.Debug;
                options.IncludeEventDataOnBreadcrumbs = true;
                options.MinimumBreadcrumbLevel = LogLevel.Debug;
                options.Dsn = ValidDsn;
                options.Release = "test-release";
                options.User = new SentryNLogUser
                {
                    Id = "${scopeproperty:item=id}",
                    Username = "${scopeproperty:item=username}",
                    Email = "${scopeproperty:item=email}",
                    IpAddress = "${scopeproperty:item=ipAddress}",
                    Segment = "${scopeproperty:item=segment}",
                    Other =
                    {
                        new TargetPropertyWithContext("mood", "joyous")
                    },
                };

                options.AddTag("logger", "${logger}");
            });

        LogManager.Configuration = configuration;

        var log = LogManager.GetCurrentClassLogger();

        using (ScopeContext.PushProperty("id", "myId"))
        {
            try
            {
                throw new("Exception message");
            }
            catch (Exception exception)
            {
                log.Error(exception, "message = {arg}", "arg value");
            }
        }

        LogManager.Flush();

        return Verify(transport.Envelopes)
            .UniqueForRuntimeAndVersion()
            .IgnoreStandardSentryMembers();
    }

    [Fact]
    public Task LoggingInsideTheContextOfLogging()
    {
        var transport = new RecordingTransport();

        var configuration = new LoggingConfiguration();

        var diagnosticLogger = new InMemoryDiagnosticLogger();
        configuration.AddSentry(
            options =>
            {
                options.TracesSampleRate = 1;
                options.Debug = true;
                options.DiagnosticLogger = diagnosticLogger;
                options.Transport = transport;
                options.Dsn = ValidDsn;
                options.AttachStacktrace = false;
                options.Release = "test-release";
            });

        LogManager.Configuration = configuration;

        var log = LogManager.GetCurrentClassLogger();

        SentrySdk.ConfigureScope(
            scope =>
            {
                scope.OnEvaluating += (_, _) => log.Error("message from OnEvaluating");
                log.Error("message");
            });
        LogManager.Flush();

        return Verify(
                new
                {
                    diagnosticLoggerEntries = diagnosticLogger
                        .Entries
                        .Where(_ => _.Level == SentryLevel.Error),
                    transport.Envelopes
                })
            .IgnoreStandardSentryMembers();
    }
}
