using Microsoft.AspNetCore;

namespace Sentry.Samples.AspnetCore21.Basic
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseSentry(o => o.Debug = true)
                .UseStartup<Startup>();
    }
}
