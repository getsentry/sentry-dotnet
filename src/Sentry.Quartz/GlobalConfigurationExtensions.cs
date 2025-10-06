using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quartz;

namespace Sentry.Quartz;

/// <summary>
/// Quartz.NET Extensions for <see cref="GlobalConfigurationExtensions"/>.
/// </summary>
public static class GlobalConfigurationExtensions
{
    /// <summary>
    /// Uses Sentry
    /// </summary>
    /// <param name="configuration"></param>
    /// <param name="services"></param>
    /// <param name="configure"></param>
    /// <returns></returns>
    public static IServiceCollectionQuartzConfigurator UseSentry(this IServiceCollectionQuartzConfigurator configuration, IServiceCollection services, Action<SentryCronJobOptions> configure)
    {
        services.AddOptions<SentryCronJobOptions>().PostConfigure(configure);

        services.AddTransient<SentryCronJobListener>();
        configuration.AddJobListener<SentryCronJobListener>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<SentryCronJobListener>>();
            var options = sp.GetRequiredService<IOptions<SentryCronJobOptions>>();

            return new SentryCronJobListener(logger, options);
        });

        return configuration;
    }

    /// <summary>
    /// For testing
    /// </summary>
    /// <param name="configuration"></param>
    /// <param name="services"></param>
    /// <param name="hub"></param>
    /// <param name="logger"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    internal static IServiceCollectionQuartzConfigurator UseSentry(this IServiceCollectionQuartzConfigurator configuration, IServiceCollection services, IHub? hub, ILogger<SentryCronJobListener> logger, IOptions<SentryCronJobOptions> options)
    {
        services.AddTransient<SentryCronJobListener>();
        configuration.AddJobListener(new SentryCronJobListener(logger, options, hub));

        return configuration;
    }
}
