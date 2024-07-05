using Microsoft.AspNetCore;

namespace Sentry.Samples.AspNetCore.Basic;

public class Program
{
    public static void Main(string[] args)
    {
        BuildWebHost(args).Run();
    }

    public static IWebHost BuildWebHost(string[] args) =>
        WebHost.CreateDefaultBuilder(args)

            .UseSentry(o =>
            {
                // A DSN is required.  You can set it here, or in configuration, or in an environment variable.
                o.Dsn = "https://203a48de94faaff13796c2a1bb37e209@o447951.ingest.us.sentry.io/4507543651352576";

                // Enable Sentry performance monitoring
                o.TracesSampleRate = 1.0;

                o.Debug = true;
                o.DiagnosticLevel = SentryLevel.Debug;

                o.SetBeforeSendTransaction(transaction =>
                {
                    if (transaction.Name.Contains("filterme"))
                    {
                        return null;
                    }

                    return transaction;
                });
            })

            // The App:
            .Configure(app =>
            {
                app.UseRouting();

                // An example ASP.NET Core middleware that throws an
                // exception when serving a request to path: /throw
                app.UseEndpoints(endpoints =>
                {
                    // Reported events will be grouped by route pattern
                    endpoints.MapGet("/throw/{message?}", context =>
                    {
                        var exceptionMessage = context.GetRouteValue("message") as string;

                        var log = context.RequestServices.GetRequiredService<ILoggerFactory>()
                            .CreateLogger<Program>();

                        log.LogInformation("Handling some request...");

                        var hub = context.RequestServices.GetRequiredService<IHub>();
                        hub.ConfigureScope(s =>
                        {
                            // More data can be added to the scope like this:
                            s.SetTag("Sample", "ASP.NET Core"); // indexed by Sentry
                            s.SetExtra("Extra!", "Some extra information");
                        });

                        log.LogInformation("Logging info...");
                        log.LogWarning("Logging some warning!");

                        // The following exception will be captured by the SDK and the event
                        // will include the Log messages and any custom scope modifications
                        // as exemplified above.
                        throw new Exception(
                            exceptionMessage ?? "An exception thrown from the ASP.NET Core pipeline");
                    });
                    endpoints.MapGet("/some/endpoint", async context =>
                    {
                        context.Response.ContentType = "application/json";
                        await context.Response.WriteAsync("{\"status\":\"Healthy\"}");
                    });
                    endpoints.MapGet("/filterme", async context =>
                    {
                        context.Response.ContentType = "application/json";
                        await context.Response.WriteAsync("{\"filter status\":\"Healthy\"}");
                    });
                    endpoints.MapGet("/healthcheck", async context =>
                    {
                        context.Response.ContentType = "application/json";
                        await context.Response.WriteAsync("{\"status\":\"Healthy\"}");
                    });
                    endpoints.MapGet("/healthcheck/database", async context =>
                    {
                        context.Response.ContentType = "application/json";
                        await context.Response.WriteAsync("{\"database status\":\"Healthy\"}");
                    });
                });
            })
            .Build();
}
