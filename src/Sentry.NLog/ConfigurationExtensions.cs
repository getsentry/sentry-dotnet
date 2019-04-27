using System;

using NLog.Config;
using NLog.Targets;

namespace Sentry.NLog
{
    public static class ConfigurationExtensions
    {
        /// <summary>
        /// Adds a target for Sentry to the NLog configuration.
        /// </summary>
        /// <param name="configuration">The NLog configuration.</param>
        /// <param name="dsn">          The sentry DSN.</param>
        /// <param name="optionsConfig">An optional configuration for the Sentry target.</param>
        /// <returns>The configuration.</returns>
        public static LoggingConfiguration AddSentryTarget(this LoggingConfiguration configuration,
                                                           string dsn,
                                                           Action<SentryNLogOptions> optionsConfig = null)
        {
            return AddSentryTarget(configuration, dsn, "sentry", optionsConfig);
        }

        /// <summary>
        /// Adds a target for Sentry to the NLog configuration.
        /// </summary>
        /// <param name="configuration">The NLog configuration.</param>
        /// <param name="dsn">          The sentry DSN.</param>
        /// <param name="targetName">   The name to give the new target.</param>
        /// <param name="optionsConfig">An optional configuration for the Sentry target.</param>
        /// <returns>The configuration.</returns>
        public static LoggingConfiguration AddSentryTarget(this LoggingConfiguration configuration,
                                                           string dsn,
                                                           string targetName,
                                                           Action<SentryNLogOptions> optionsConfig = null)
        {
            Target.Register<SentryTarget>("Sentry");

            var options = new SentryNLogOptions
            {
                Dsn = new Dsn(dsn)
            };
            optionsConfig?.Invoke(options);
            configuration?.AddTarget(targetName, new SentryTarget(options));
            return configuration;
        }
    }
}
