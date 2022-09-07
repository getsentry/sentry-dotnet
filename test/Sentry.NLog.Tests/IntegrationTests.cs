using NLog;
using NLog.Config;
using NLog.Targets;
using Sentry.Testing;

namespace Sentry.NLog.Tests;

[UsesVerify]
public class IntegrationTests
{
    [Fact]
    public Task Simple()
    {
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
                options.User = new SentryNLogUser
                {
                    Id = "${scopeproperty:item=id}",
                    Username = "${scopeproperty:item=username}",
                    Email = "${scopeproperty:item=email}",
                    IpAddress = "${scopeproperty:item=ipAddress}",
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
