using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Sentry.AspNetCore;
using Sentry.Serilog.Tests.Utils;
using Sentry.Testing;
using Serilog;

namespace Sentry.Serilog.Tests;

// Allows integration tests the include the background worker and mock only the HTTP bits
public class AspNetSentrySdkTestFixture : SentrySdkTestFixture
{
    protected SentryEvent LastEvent;
    protected List<SentryEvent> Events;

    protected Action<SentryAspNetCoreOptions> Configure;

    protected Action<WebHostBuilder> AfterConfigureBuilder;

    protected override void ConfigureBuilder(WebHostBuilder builder)
    {
        Events = new List<SentryEvent>();
        Configure = options =>
        {
            options.BeforeSend = @event =>
            {
                Events.Add(@event);
                LastEvent = @event;
                return @event;
            };
        };

        ConfigureApp = app =>
        {
#if NETCOREAPP3_1_OR_GREATER
            app.UseExceptionHandler(new ExceptionHandlerOptions
            {
#if NET6_0_OR_GREATER
                AllowStatusCode404Response = true,
#endif
                ExceptionHandlingPath = "/error"
            });
#endif
        };

        var sentry = FakeSentryServer.CreateServer();
        var sentryHttpClient = sentry.CreateClient();
        _ = builder.UseSentry(options =>
        {
            options.Dsn = DsnSamples.ValidDsnWithSecret;
            options.SentryHttpClientFactory = new DelegateHttpClientFactory(_ => sentryHttpClient);

            Configure?.Invoke(options);
        });

        builder.ConfigureLogging(loggingBuilder =>
        {
            var logger = new LoggerConfiguration()
                .WriteTo.Sentry()
                .CreateLogger();
            loggingBuilder.AddSerilog(logger);
        });

        AfterConfigureBuilder?.Invoke(builder);
    }
}
