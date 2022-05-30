using Sentry.AspNetCore;

namespace Sentry.Samples.AspNetCore5.Mvc;

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
                    o.Dsn = "https://eb18e953812b41c3aeb042e666fd3b5c@o447951.ingest.sentry.io/5428537";
                    o.TracesSampler = ctx =>
                    {
                        // This is an example of using a custom HTTP request header to make the sampling decision
                        var httpContext = ctx.TryGetHttpContext()!;
                        var requestHeaders = httpContext.Request.Headers;
                        if (requestHeaders.ContainsKey("X-Sentry-No-Tracing"))
                        {
                            return 0;
                        }

                        // This is an example of changing the sample rate for a specific HTTP route
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
