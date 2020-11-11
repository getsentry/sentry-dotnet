using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

// Only used for testing locally: dotnet run
public static class LocalEntryPoint
{
    public static void Main(string[] args) => CreateHostBuilder(args).Build().Run();

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>()
                    // Add Sentry (configuration was done via appsettings.json but could be done programatically):
                    .UseSentry();
            });
}
