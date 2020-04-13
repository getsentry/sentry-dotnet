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
                        o.Dsn = "https://5fd7a6cda8444965bade9ccfd3df9882@sentry.io/1188141";
                    });
                    webBuilder.UseStartup<Startup>();
                });
    }
}
