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
                webBuilder.UseStartup<Startup>();
            })
            .UseSentry(o =>
            {
                o.Debug = true;
                o.MaxRequestBodySize = RequestSize.Always;
                o.Dsn = "https://eb18e953812b41c3aeb042e666fd3b5c@o447951.ingest.sentry.io/5428537";
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
}
