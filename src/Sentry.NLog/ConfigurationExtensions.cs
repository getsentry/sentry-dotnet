using System;

using NLog.Config;
using NLog.Targets;

using Sentry;
using Sentry.NLog;

// ReSharper disable once CheckNamespace
namespace NLog
{
    public static class ConfigurationExtensions
    {
        /// <summary>
        /// Adds a target for Sentry to the NLog configuration.
        /// </summary>
        /// <remarks>
        /// If DSN is not set, the SDK will look for an environment variable called SENTRY_DSN. If nothing is
        /// found, SDK is disabled.
        /// </remarks>
        /// <param name="configuration">The NLog configuration.</param>
        /// <param name="optionsConfig">An optional configuration for the Sentry target.</param>
        /// <returns>The configuration.</returns>
        public static LoggingConfiguration AddSentryTarget(this LoggingConfiguration configuration,
                                                           Action<SentryNLogOptions> optionsConfig = null)
        {
            return AddSentryTarget(configuration, null, "sentry", optionsConfig);
        }

        /// <summary>
        /// Adds a target for Sentry to the NLog configuration.
        /// </summary>
        /// <param name="configuration">The NLog configuration.</param>
        /// <param name="dsn">          
        /// The sentry DSN. If DSN is not set, the SDK will look for an environment variable called SENTRY_DSN.
        /// If nothing is found, SDK is disabled.
        /// </param>
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

            var options = new SentryNLogOptions();

            optionsConfig?.Invoke(options);

            if (dsn != null && options.Dsn == null)
                options.Dsn = new Dsn(dsn);

            configuration?.AddTarget(targetName, new SentryTarget(options)
            {
                Name = targetName,
                Layout = "${message}",
            });

            configuration?.AddRuleForAllLevels(targetName);

            return configuration;
        }

    }
}
