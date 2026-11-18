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

        using (SentrySdk.Init(
                   options =>
                   {
                       options.TracesSampleRate = 1;
                       options.Transport = transport;
                       options.DiagnosticLevel = SentryLevel.Debug;
                       options.Dsn = ValidDsn;
                       options.Release = "test-release";
                   }))
        {
            var configuration = new LoggingConfiguration();

            configuration.AddSentry(
                options =>
                {
                    options.Layout = "${message}";
                    options.IncludeEventDataOnBreadcrumbs = true;
                    options.MinimumBreadcrumbLevel = LogLevel.Debug;
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

            var log = LogManager.GetLogger("MyApp.IntegrationTests");

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
        }

        return Verify(transport.Envelopes)
            .UniqueForRuntimeAndVersion()
            .IgnoreStandardSentryMembers();
    }

    [Fact]
    public void UserLayouts_DataCollectionUserInfoDisabled_UserStillSent()
    {
        var transport = new RecordingTransport();

        using (SentrySdk.Init(
                   options =>
                   {
                       options.Transport = transport;
                       options.Dsn = ValidDsn;
                       options.ConfigureDataCollection(dataCollection => dataCollection.UserInfo = false);
                   }))
        {
            var configuration = new LoggingConfiguration();
            configuration.AddSentry(
                options => options.User = new SentryNLogUser
                {
                    Id = "myId",
                    Username = "myUsername",
                    Email = "me@example.com",
                    IpAddress = "192.0.2.1",
                });
            LogManager.Configuration = configuration;

            LogManager.GetLogger("MyApp.IntegrationTests").Error("message");
            LogManager.Flush();
        }

        var user = transport.Payloads
            .OfType<JsonSerializable>()
            .Select(payload => payload.Source)
            .OfType<SentryEvent>()
            .Single()
            .User;
        user.Id.Should().Be("myId");
        user.Username.Should().Be("myUsername");
        user.Email.Should().Be("me@example.com");
        user.IpAddress.Should().Be("192.0.2.1");
    }

    [Fact]
    public Task LoggingInsideTheContextOfLogging()
    {
        var transport = new RecordingTransport();
        var diagnosticLogger = new InMemoryDiagnosticLogger();

        using (SentrySdk.Init(
                   options =>
                   {
                       options.TracesSampleRate = 1;
                       options.Debug = true;
                       options.DiagnosticLogger = diagnosticLogger;
                       options.Transport = transport;
                       options.Dsn = ValidDsn;
                       options.AttachStacktrace = false;
                       options.Release = "test-release";
                   }))
        {
            var configuration = new LoggingConfiguration();
            configuration.AddSentry();

            LogManager.Configuration = configuration;

            var log = LogManager.GetLogger("MyApp.IntegrationTests");

            SentrySdk.ConfigureScope(
                scope =>
                {
                    scope.OnEvaluating += (_, _) => log.Error("message from OnEvaluating");
                    log.Error("message");
                });
            LogManager.Flush();
        }

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
