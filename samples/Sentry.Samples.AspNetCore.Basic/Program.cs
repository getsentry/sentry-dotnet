using System;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

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
                .Configure(a =>
                {
                    a.Use(async (context, next) =>
                    {
                        var log = context.RequestServices.GetService<ILoggerFactory>()
                            .CreateLogger<Program>();

                        log.LogInformation("Handling some request...");

                        if (context.Request.Path == "/throw")
                        {

                            log.LogWarning("Throwing an exception!");
                            throw new Exception("An exception thrown from the ASP.NET Core pipeline");
                        }

                        await next();
                    });
                })
                // Add Sentry integration
                .UseSentry()
                .Build();
    }
}
