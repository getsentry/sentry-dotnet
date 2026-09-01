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
    /// Adds middleware to track CRON jobs to Sentry
    /// </summary>
    /// <param name="configuration"></param>
    /// <param name="configure"></param>
    /// <param name="serviceCollection"></param>
    /// <returns></returns>
    public static IQuartzBuilder AddSentryCronJob(this IQuartzBuilder configuration, IServiceCollection serviceCollection, Action<SentryCronJobOptions>? configure = null)
    {
        if (configure is not null)
        {
            serviceCollection.Configure(configure);
        }

        return configuration.AddJobMiddleware<SentryCronJobMiddleware>();
    }

    /// <summary>
    /// Adds middleware to track job execution duration metrics
    /// </summary>
    /// <param name="configuration"></param>
    /// <returns></returns>
    public static IQuartzBuilder AddSentryMetrics(this IQuartzBuilder configuration)
    {
        return configuration.AddJobMiddleware<SentryMetricsMiddleware>();
    }

    /// <summary>
    /// For testing
    /// </summary>
    /// <param name="configuration"></param>
    /// <param name="hub"></param>
    /// <param name="options"></param>
    /// <param name="logger"></param>
    /// <returns></returns>
    internal static IQuartzBuilder AddSentryCronJob(this IQuartzBuilder configuration, IOptions<SentryCronJobOptions> options, IHub hub, ILogger<SentryCronJobMiddleware> logger)
    {
        configuration.AddJobMiddleware(new SentryCronJobMiddleware(options, hub, logger));
        return configuration;
    }
}
