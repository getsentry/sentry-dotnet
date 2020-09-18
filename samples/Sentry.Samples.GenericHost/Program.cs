using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sentry;

internal static class Program
{
    public static Task Main()
        => new HostBuilder()
            .ConfigureHostConfiguration(c =>
            {
                c.SetBasePath(Directory.GetCurrentDirectory());
                c.AddJsonFile("appsettings.json", optional: false);
            })
            .ConfigureServices((c, s) => { s.AddHostedService<SampleHostedService>(); })
            .ConfigureLogging((c, l) =>
            {
                l.AddConfiguration(c.Configuration);
                l.AddConsole();
                l.AddSentry();
            })
            .UseConsoleLifetime()
            .Build()
            .RunAsync();

    internal class SampleHostedService : IHostedService
    {
        private readonly IHub _hub;
        private readonly ILogger _logger;

        public SampleHostedService(IHub hub, ILogger<SampleHostedService> logger)
        {
            _hub = hub;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            // Logging integration by default keeps informational logs as Breadcrumb
            _logger.LogInformation("Starting sample hosted service. This goes as a breadcrumb");
            // You can also add breadcrumb directly through Sentry.Hub:
            _hub.AddBreadcrumb("Breadcrumb added directly to Sentry Hub")
                ;
            // Hub allows total control of the scope
            _hub.ConfigureScope(s => s.SetTag("Worker", nameof(SampleHostedService)));

            // By default Error and Critical log messages are sent to sentry as events
            _logger.LogError("An event sent to sentry.");

            return Task.Run(() =>
            {
                try
                {
                    var zero = 0;
                    _ = 10 / zero; // Throws DivideByZeroException
                }
                catch (Exception e)
                {
                    // Direct control of capturing errors with Sentry
                    _hub.CaptureException(e);
                }
            }, cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping sample hosted service.");
            return Task.CompletedTask;
        }
    }
}
