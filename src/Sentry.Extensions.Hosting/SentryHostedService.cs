using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Sentry.Extensions.Hosting;

internal sealed class SentryHostedService : IHostedService
{
    private const string MissingAspNetCoreIntegration =
        "This is an ASP.NET Core app, but UseSentry from Sentry.Extensions.Hosting doesn't include the ASP.NET Core " +
        "integration: events have no request data and requests aren't traced. Call UseSentry from Sentry.AspNetCore " +
        "on the WebApplicationBuilder instead. In a helper that takes any IHostApplicationBuilder, such as Aspire's " +
        "AddServiceDefaults, use: if (builder is WebApplicationBuilder web) " +
        "SentryWebApplicationBuilderExtensions.UseSentry(web); else builder.UseSentry(); Call it as a static method: " +
        "in namespace Microsoft.Extensions.Hosting, where ServiceDefaults is declared, web.UseSentry() still picks " +
        "this one.";

    private readonly IHostApplicationLifetime _lifetime;
    private readonly ILogger<SentryHostedService> _logger;
    private readonly bool _isWebAppWithoutAspNetCoreIntegration;

    public SentryHostedService(IServiceProvider services, bool isWebAppWithoutAspNetCoreIntegration)
    {
        _ = services.GetRequiredService<Func<IHub>>();
        _lifetime = services.GetRequiredService<IHostApplicationLifetime>();
        _logger = services.GetRequiredService<ILogger<SentryHostedService>>();
        _isWebAppWithoutAspNetCoreIntegration = isWebAppWithoutAspNetCoreIntegration;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (_isWebAppWithoutAspNetCoreIntegration)
        {
            _logger.LogWarning(MissingAspNetCoreIntegration);
        }

        _lifetime.ApplicationStopped.Register(SentrySdk.Close);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
