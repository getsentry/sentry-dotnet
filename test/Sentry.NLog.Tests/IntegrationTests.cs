using NLog;
using NLog.Common;
using NLog.Config;
using NLog.Filters;
using NLog.Targets;

namespace Sentry.NLog.Tests;

[UsesVerify]
public class IntegrationTests
{
    [Fact]
    public async Task Simple()
    {
        var transport = new RecordingTransport();

        var nlogConfiguration = new LoggingConfiguration();

        nlogConfiguration.AddSentry(o =>
        {
            o.TracesSampleRate = 1;
            o.Layout = "${message}";
            o.Transport = transport;
            o.DiagnosticLevel = SentryLevel.Debug;
            o.IncludeEventDataOnBreadcrumbs = true;
            o.MinimumBreadcrumbLevel = LogLevel.Debug;
            o.Dsn = ValidDsn;
            o.User = new SentryNLogUser
            {
                Id = "${mdlc:item=id}",
                Username = "${mdlc:item=username}",
                Email = "${mdlc:item=email}",
                IpAddress = "${mdlc:item=ipAddress}",
                Other =
                {
                    new TargetPropertyWithContext("mood", "joyous")
                },
            };

            o.AddTag("logger", "${logger}");
        });

        LogManager.Configuration = nlogConfiguration;

        var log = LogManager.GetCurrentClassLogger();

        using (MappedDiagnosticsLogicalContext.SetScoped("id", "myId"))
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

        await Verify(transport.Envelopes)
            .IgnoreStandardSentryMembers();
    }

    [Fact]
    public async Task LoggingInsideTheContextOfLogging()
    {
        var transport = new RecordingTransport();

        var nlogConfiguration = new LoggingConfiguration();

        nlogConfiguration.AddSentry(o =>
        {
            o.TracesSampleRate = 1;
            o.Layout = "${message}";
            o.Transport = transport;
            o.DiagnosticLevel = SentryLevel.Debug;
            o.IncludeEventDataOnBreadcrumbs = true;
            o.MinimumBreadcrumbLevel = LogLevel.Debug;
            o.Dsn = ValidDsn;
        });

        var rule = new LoggingRule("*", LogLevel.Debug, nlogConfiguration.AllTargets.Single());
        rule.Filters.Add(new FilterThatLogs());

        nlogConfiguration.LoggingRules.Add(rule);

        LogManager.Configuration = nlogConfiguration;

        var log = LogManager.GetCurrentClassLogger();

        log.Error("message");

        LogManager.Flush();

        await Verify(transport.Envelopes)
            .IgnoreStandardSentryMembers();
    }

    public class FilterThatLogs : Filter
    {
        public FilterThatLogs()
        {
            Action = FilterResult.Log;
        }

        protected override FilterResult Check(LogEventInfo logEvent)
        {
            var log = LogManager.GetCurrentClassLogger();
            log.Error("message from filter");
            return FilterResult.Log;
        }
    }
}
