using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Sentry.AspNetCore;
using Sentry.Extensibility;

namespace Sentry.Samples.AspNetCore5.Mvc
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseSentry(o =>
                    {
                        o.Debug = true;
                        o.MaxRequestBodySize = RequestSize.Always;
                        o.Dsn = "https://80aed643f81249d4bed3e30687b310ab@o447951.ingest.sentry.io/5428537";
                        o.TracesSampler = ctx =>
                        {
                            if (string.Equals(ctx.TryGetHttpRoute(), "/Home/Privacy", StringComparison.Ordinal))
                            {
                                // Collect fewer traces for this page
                                return 0.3;
                            }

                            return 1;
                        };
                    });
                    webBuilder.UseStartup<Startup>();
                });
    }
}
