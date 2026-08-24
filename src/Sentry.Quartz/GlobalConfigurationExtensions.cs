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
    /// <param name="configure"></param>
    /// <param name="serviceCollection"></param>
    /// <returns></returns>
    public static IQuartzBuilder UseSentry(this IQuartzBuilder configuration, IServiceCollection serviceCollection, Action<SentryCronJobOptions>? configure = null)
    {
        serviceCollection.AddSingleton<SentryCronJobListener>();

        if (configure is not null)
        {
            serviceCollection.Configure(configure);
        }

        configuration.AddJobListener<SentryCronJobListener>(sp =>
        {
            var serviceScope = sp.CreateScope();
            var serviceProvider = serviceScope.ServiceProvider;

            return serviceProvider.GetRequiredService<SentryCronJobListener>();
        });

        return configuration;
    }

    /// <summary>
    /// For testing
    /// </summary>
    /// <param name="configuration"></param>
    /// <param name="hub"></param>
    /// <param name="options"></param>
    /// <param name="logger"></param>
    /// <returns></returns>
    internal static IQuartzBuilder UseSentry(this IQuartzBuilder configuration, IOptions<SentryCronJobOptions> options, IHub hub, ILogger<SentryCronJobListener> logger)
    {
        configuration.AddJobListener(new SentryCronJobListener(options, hub, logger));
        return configuration;
    }
}
