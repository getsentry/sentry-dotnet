using NLog;
using NLog.Config;
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

        var payloads = transport.Envelopes
            .SelectMany(x => x.Items)
            .Select(x => x.Payload)
            .ToList();

        await Verify(payloads)
            .IgnoreStandardSentryMembers();
    }
}
