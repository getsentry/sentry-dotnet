using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Sentry.Samples.AspNetCore.Mvc
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseShutdownTimeout(TimeSpan.FromSeconds(10))
                .UseStartup<Startup>()

                // Example integration with advanced configuration scenarios:
                .UseSentry(options =>
                {
                    // The parameter 'options' here has values populated through the configuration system.
                    // That includes 'appsettings.json', environment variables and anything else
                    // defined on the ConfigurationBuilder.
                    // See: https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-2.1&tabs=basicconfiguration
                    options.Init(i =>
                    {
                        i.MaxBreadcrumbs = 200;

                        i.Http(h =>
                        {
                            //h.Proxy = new WebProxy("https://localhost:3128");
                            h.AcceptDeflate = false;
                            h.AcceptGzip = false;
                        });

                        i.Worker(w =>
                        {
                            w.MaxQueueItems = 100;
                            w.ShutdownTimeout = TimeSpan.FromSeconds(5);
                        });
                    });

                    // Hard-coding here will override any value set on appsettings.json:
                    options.Logging.MinimumEventLevel = LogLevel.Error;
                });
    }
}
