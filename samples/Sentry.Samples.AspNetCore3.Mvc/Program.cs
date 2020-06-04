using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sentry.Extensibility;

namespace Sentry.Samples.AspNetCore3.Mvc
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
                        o.Dsn = "https://9f271c100c3248a4b074a0bead837061@o19635.ingest.sentry.io/5264714";
                    });
                    webBuilder.UseStartup<Startup>();
                });
    }
}
