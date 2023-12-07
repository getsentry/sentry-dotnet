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
                o.Dsn = "https://b887218a80114d26a9b1a51c5f88e0b4@o447951.ingest.sentry.io/6601807";

                // Enable Sentry performance monitoring
                o.EnableTracing = true;

                o.ExperimentalMetrics = new ExperimentalMetricsOptions(){ MetricSampleRate = 1.0 };
#if DEBUG
                // Log debug information about the Sentry SDK
                o.Debug = true;
#endif
            })

            // The App:
            .Configure(app =>
            {
                app.UseRouting();

                // An example ASP.NET Core middleware that throws an
                // exception when serving a request to path: /throw
                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapGet("/hello", () =>
                    {
                        SentrySdk.Metrics.Increment("hello.world");
                        return "Hello World!";
                    });
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
                });
            })
            .Build();
}
