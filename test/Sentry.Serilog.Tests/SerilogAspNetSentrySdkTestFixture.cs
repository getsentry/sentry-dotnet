namespace Sentry.Serilog.Tests;

public class SerilogAspNetSentrySdkTestFixture : AspNetSentrySdkTestFixture
{
    protected List<SentryEvent> Events;

    protected override void ConfigureBuilder(WebHostBuilder builder)
    {
        Events = new();
        Configure = options =>
        {
            options.BeforeSend = @event =>
            {
                Events.Add(@event);
                return @event;
            };
        };

        ConfigureApp = app =>
        {
            app.UseExceptionHandler(new ExceptionHandlerOptions
            {
#if NET6_0_OR_GREATER
                AllowStatusCode404Response = true,
#endif
                ExceptionHandlingPath = "/error"
            });
        };

        builder.ConfigureLogging(loggingBuilder =>
        {
            var logger = new LoggerConfiguration()
                .WriteTo.Sentry()
                .CreateLogger();
            loggingBuilder.AddSerilog(logger);
        });

        base.ConfigureBuilder(builder);
    }
}
