using Quartz;
using Sentry.Extensibility;

namespace Sentry.Quartz;

/// <summary>
/// Sentry extensions for <see cref="IQuartzBuilder"/>.
/// </summary>
public static class SentryQuartzBuilderExtensions
{
    extension(IQuartzBuilder builder)
    {
        /// <summary>
        /// Adds Sentry to the scheduler's job execution pipeline.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Each job runs in its own Sentry scope and trace, tagged with the job's key, unless global mode is enabled.
        /// Exceptions that jobs throw are not captured here: Quartz logs them, and Sentry's logging integration
        /// captures that log entry.
        /// </para>
        /// <para>
        /// Jobs with <see cref="SentryCronMonitorSlugAttribute"/> also send a check-in each time they run, with the
        /// schedule of the trigger that fired them as the monitor config. A job that throws or is cancelled sends an
        /// error check-in. See <see cref="SentryQuartzOptions.SendMonitorConfig"/>.
        /// </para>
        /// </remarks>
        /// <param name="configure">Configures the options.</param>
        /// <returns>The builder.</returns>
        public IQuartzBuilder AddSentry(Action<SentryQuartzOptions>? configure = null)
        {
            var options = new SentryQuartzOptions();
            configure?.Invoke(options);
            return builder.AddSentry(options, HubAdapter.Instance);
        }

        internal IQuartzBuilder AddSentry(SentryQuartzOptions options, IHub hub, IDiagnosticLogger? logger = null)
        {
            // Middleware runs outermost first, so check-ins are captured in the job's scope and trace
            builder.AddJobMiddleware(new SentryScopeMiddleware(hub));
            builder.AddJobMiddleware(new SentryCronJobMiddleware(new JobMonitor(options, hub, logger)));
            return builder;
        }
    }
}
