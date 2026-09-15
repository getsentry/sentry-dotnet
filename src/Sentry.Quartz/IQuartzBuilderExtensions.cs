using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quartz;

namespace Sentry.Quartz;

/// <summary>
/// Quartz.NET Extensions for <see cref="IQuartzBuilderExtensions"/>.
/// </summary>
public static class IQuartzBuilderExtensions
{
    extension(IQuartzBuilder configuration)
    {
        /// <summary>
        /// Adds middleware to track CRON jobs to Sentry
        /// </summary>
        /// <param name="configure">Configures the options</param>
        /// <returns></returns>
        public IQuartzBuilder AddSentryCronJobs(Action<SentryCronJobOptions>? configure = null)
        {
            return configuration.AddJobMiddleware<SentryCronJobMiddleware>().ConfigureOptions(configure);
        }

        /// <summary>
        /// Adds middleware that pushes a scope to sentry before job execution
        /// </summary>
        /// <param name="configure">Configures the options</param>
        /// <returns></returns>
        public IQuartzBuilder AddSentryScope(Action<SentryScopeMiddlewareOptions>? configure = null)
        {
            return configuration.AddJobMiddleware<SentryScopeMiddleware>().ConfigureOptions(configure);
        }

        /// <summary>
        /// For testing
        /// </summary>
        /// <param name="options"></param>
        /// <param name="hub"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        internal IQuartzBuilder AddSentryCronJobs(IOptions<SentryCronJobOptions> options, IHub hub, ILogger<SentryCronJobMiddleware> logger)
        {
            configuration.AddJobMiddleware(new SentryCronJobMiddleware(hub, options, logger));
            return configuration;
        }
    }
}
