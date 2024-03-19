using System.Text;
using Microsoft.AspNetCore;
using Sentry.Profiling;

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
                o.Dsn = "https://eb18e953812b41c3aeb042e666fd3b5c@o447951.ingest.sentry.io/5428537";

                // Enable Sentry performance monitoring
                o.EnableTracing = true;

                // Make sure to reduce the sampling rate in production.
                o.ProfilesSampleRate = 1.0;

                o.AddIntegration(new ProfilingIntegration(TimeSpan.FromMilliseconds(5000)));
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

                    // Reported events will be grouped by route pattern
                    endpoints.MapGet("/", context =>
                    {
                        var sb = new StringBuilder();
                        for (var i = 0; i < 10; i++)
                        {
                            sb.AppendLine(FindPrimeNumber(10000).ToString());
                        }
                        return Task.FromResult(sb.ToString());
                    });
                });
            })
            .Build();

    private static long FindPrimeNumber(int n)
    {
        int count = 0;
        long a = 2;
        while (count < n)
        {
            long b = 2;
            int prime = 1;// to check if found a prime
            while (b * b <= a)
            {
                if (a % b == 0)
                {
                    prime = 0;
                    break;
                }
                b++;
            }
            if (prime > 0)
            {
                count++;
            }
            a++;
        }
        return (--a);
    }
}
