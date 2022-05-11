using Microsoft.Extensions.Hosting;
using Sentry.Extensions.Logging.Internal;

namespace Sentry.Extensions.Logging;

internal class SentryStartupService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;

    public SentryStartupService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _serviceProvider.ConfigureSentryOptions<SentryLoggingOptions>();
        _serviceProvider.RegisterSentrySdkClose();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
