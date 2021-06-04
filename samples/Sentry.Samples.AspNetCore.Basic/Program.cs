using System;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Sentry.AspNetCore;

namespace Sentry.Samples.AspNetCore.Basic
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BuildWebHost(args).Run();
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)

                // Add Sentry integration
                // In this example, DSN and Release are set via environment variable:
                // See: Properties/launchSettings.json
                .UseSentry()
                // It can also be defined via configuration (including appsettings.json)
                // or coded explicitly, via parameter like:
                // .UseSentry("dsn") or .UseSentry(o => o.Dsn = ""; o.Release = "1.0"; ...)

                // The App:
                .Configure(app =>
                {
                    // An example ASP.NET Core middleware that throws an
                    // exception when serving a request to path: /throw
                    app.UseRouting();

                    // Enable Sentry performance monitoring
                    app.UseSentryTracing();

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
                                exceptionMessage ?? "An exception thrown from the ASP.NET Core pipeline"
                            );
                        });
                    });
                })
                .Build();
    }
}
