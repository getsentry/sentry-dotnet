using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Sentry.Samples.GenericHost;

internal class SampleHostedService(IHub hub, ILogger<SampleHostedService> logger) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Logging integration by default keeps informational logs as Breadcrumb
        logger.LogInformation("Starting sample hosted service. This goes as a breadcrumb");
        // You can also add breadcrumb directly through Sentry.Hub:
        hub.AddBreadcrumb("Breadcrumb added directly to Sentry Hub");
        // Hub allows total control of the scope
        hub.ConfigureScope(s => s.SetTag("Worker", nameof(SampleHostedService)));

        // By default, Error and Critical log messages are sent to sentry as events
        logger.LogError("An event sent to sentry.");

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
                hub.CaptureException(e);
            }
        }, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Stopping sample hosted service.");
        return Task.CompletedTask;
    }
}
