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
        if (configure is not null)
        {
            serviceCollection.Configure(configure);
        }

        return configuration.AddJobMiddleware<SentryCronJobMiddleware>();
    }

    /// <summary>
    /// For testing
    /// </summary>
    /// <param name="configuration"></param>
    /// <param name="hub"></param>
    /// <param name="options"></param>
    /// <param name="logger"></param>
    /// <returns></returns>
    internal static IQuartzBuilder UseSentry(this IQuartzBuilder configuration, IOptions<SentryCronJobOptions> options, IHub hub, ILogger<SentryCronJobMiddleware> logger)
    {
        configuration.AddJobMiddleware(new SentryCronJobMiddleware(options, hub, logger));
        return configuration;
    }
}
