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
    /// <returns></returns>
    public static IServiceCollectionQuartzConfigurator UseSentry(this IServiceCollectionQuartzConfigurator configuration, Action<SentryCronJobOptions>? configure = null)
    {
        configuration.AddJobListener<SentryCronJobListener>(sp =>
        {
            var options = new SentryCronJobOptions();
            configure?.Invoke(options);
            return new SentryCronJobListener(options);
        });

        return configuration;
    }

    /// <summary>
    /// For testing
    /// </summary>
    /// <param name="configuration"></param>
    /// <param name="hub"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    internal static IServiceCollectionQuartzConfigurator UseSentry(this IServiceCollectionQuartzConfigurator configuration, IHub? hub, SentryCronJobOptions options)
    {
        configuration.AddJobListener(new SentryCronJobListener(options, hub));
        return configuration;
    }
}
