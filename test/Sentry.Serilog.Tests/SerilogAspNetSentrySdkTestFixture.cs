#if NET6_0_OR_GREATER
using Sentry.AspNetCore.TestUtils;

namespace Sentry.Serilog.Tests;

public class SerilogAspNetSentrySdkTestFixture : AspNetSentrySdkTestFixture
{
    protected List<SentryEvent> Events;
    protected List<SentryLog> Logs;

    protected bool ExperimentalEnableLogs { get; set; }

    protected override void ConfigureBuilder(WebHostBuilder builder)
    {
        Events = new List<SentryEvent>();
        Logs = new List<SentryLog>();

        Configure = options =>
        {
            options.SetBeforeSend((@event, _) => { Events.Add(@event); return @event; });

            options.Experimental.EnableLogs = ExperimentalEnableLogs;
            options.Experimental.SetBeforeSendLog(log => { Logs.Add(log); return log; });
        };

        ConfigureApp = app =>
        {
            app.UseExceptionHandler(new ExceptionHandlerOptions
            {
                AllowStatusCode404Response = true,
                ExceptionHandlingPath = "/error"
            });
        };

        builder.ConfigureLogging(loggingBuilder =>
        {
            var logger = new LoggerConfiguration()
                .WriteTo.Sentry(ValidDsn, experimentalEnableLogs: ExperimentalEnableLogs)
                .CreateLogger();
            loggingBuilder.AddSerilog(logger);
        });

        base.ConfigureBuilder(builder);
    }
}
#endif
