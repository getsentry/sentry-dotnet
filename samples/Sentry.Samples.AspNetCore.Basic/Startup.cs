using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sentry.AspNetCore;

namespace Sentry.Samples.AspNetCore.Basic
{
    public class Startup
    {
        public IConfiguration _configuration { get; }

        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRouting();
        }

        public void Configure(IApplicationBuilder app)
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
        }
    }
}
