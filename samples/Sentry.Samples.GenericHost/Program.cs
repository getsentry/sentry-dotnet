using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

await Host.CreateDefaultBuilder()
    .ConfigureHostConfiguration(c =>
    {
        c.SetBasePath(Directory.GetCurrentDirectory());
        c.AddJsonFile("appsettings.json", optional: false);
    })
    .ConfigureServices((_, s) => s.AddHostedService<SampleHostedService>())
    .ConfigureLogging((c, l) =>
    {
        l.AddConfiguration(c.Configuration);
        l.AddConsole();
        l.AddSentry();
    })
    .UseConsoleLifetime()
    .Build()
    .RunAsync();
