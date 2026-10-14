using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Sentry.Extensions.Hosting;

internal sealed class SentryHostedService : IHostedService
{
    private readonly IHostApplicationLifetime _lifetime;

    public SentryHostedService(IServiceProvider services, IHostApplicationLifetime lifetime)
    {
        _ = services.GetRequiredService<Func<IHub>>();
        _lifetime = lifetime;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _lifetime.ApplicationStopped.Register(SentrySdk.Close);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
