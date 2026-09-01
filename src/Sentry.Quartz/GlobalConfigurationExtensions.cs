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
    /// <param name="configure">Configures the options</param>
    /// <returns></returns>
    public static IQuartzBuilder AddSentryCronJobs(this IQuartzBuilder configuration, Action<SentryCronJobOptions>? configure = null)
    {
        configuration.ConfigureOptions(configure);
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
    /// Adds middleware that pushes a scope to sentry before job execution
    /// </summary>
    /// <param name="configuration"></param>
    /// <returns></returns>
    public static IQuartzBuilder AddSentryScope(this IQuartzBuilder configuration)
    {
        return configuration.AddJobMiddleware<SentryScopeMiddleware>();
    }

    /// <summary>
    /// For testing
    /// </summary>
    /// <param name="configuration"></param>
    /// <param name="hub"></param>
    /// <param name="options"></param>
    /// <param name="logger"></param>
    /// <returns></returns>
    internal static IQuartzBuilder AddSentryCronJobs(this IQuartzBuilder configuration, IOptions<SentryCronJobOptions> options, IHub hub, ILogger<SentryCronJobMiddleware> logger)
    {
        configuration.AddJobMiddleware(new SentryCronJobMiddleware(hub, options, logger));
        return configuration;
    }
}
