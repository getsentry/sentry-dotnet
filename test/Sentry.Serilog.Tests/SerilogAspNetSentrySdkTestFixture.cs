#if NET6_0_OR_GREATER
using Sentry.AspNetCore.TestUtils;

namespace Sentry.Serilog.Tests;

public class SerilogAspNetSentrySdkTestFixture : AspNetSentrySdkTestFixture
{
    protected List<SentryEvent> Events;

    protected override void ConfigureBuilder(WebHostBuilder builder)
    {
        Events = new List<SentryEvent>();
        Configure = options =>
        {
            options.SetBeforeSend((@event, _) => { Events.Add(@event); return @event; });
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
                .WriteTo.Sentry(ValidDsn)
                .CreateLogger();
            loggingBuilder.AddSerilog(logger);
        });

        base.ConfigureBuilder(builder);
    }
}
#endif
