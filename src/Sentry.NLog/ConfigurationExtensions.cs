using System;

using NLog.Config;
using NLog.Layouts;
using NLog.Targets;

using Sentry;
using Sentry.NLog;

// ReSharper disable once CheckNamespace
namespace NLog
{
    /// <summary>
    /// NLog configuration extensions.
    /// </summary>
    public static class ConfigurationExtensions
    {
        // Internal for testability
        internal const string DefaultTargetName = "sentry";

        /// <summary>
        /// Adds a target for Sentry to the NLog configuration.
        /// </summary>
        /// <remarks>
        /// If DSN is not set, the SDK will look for an environment variable called SENTRY_DSN. If nothing is
        /// found, SDK is disabled.
        /// </remarks>
        /// <param name="configuration">The NLog configuration.</param>
        /// <param name="optionsConfig">An optional action for configuring the Sentry target options.</param>
        /// <returns>The configuration.</returns>
        public static LoggingConfiguration AddSentry(this LoggingConfiguration configuration,
                                                           Action<SentryNLogOptions> optionsConfig = null)
        {
            return configuration.AddSentry(null, DefaultTargetName, optionsConfig);
        }

        /// <summary>
        /// Adds a target for Sentry to the NLog configuration.
        /// </summary>
        /// <param name="configuration">The NLog configuration.</param>
        /// <param name="dsn">
        /// The sentry DSN. If DSN is not set, the SDK will look for an environment variable called SENTRY_DSN.
        /// If nothing is found, SDK is disabled.
        /// </param>
        /// <param name="optionsConfig">An optional action for configuring the Sentry target options.</param>
        /// <returns>The configuration.</returns>
        public static LoggingConfiguration AddSentry(this LoggingConfiguration configuration,
                                                           string dsn,
                                                           Action<SentryNLogOptions> optionsConfig = null)
        {
            return configuration.AddSentry(dsn, DefaultTargetName, optionsConfig);
        }

        /// <summary>
        /// Adds a target for Sentry to the NLog configuration.
        /// </summary>
        /// <param name="configuration">The NLog configuration.</param>
        /// <param name="dsn">          The sentry DSN.</param>
        /// <param name="targetName">   The name to give the new target.</param>
        /// <param name="optionsConfig">An optional action for configuring the Sentry target options.</param>
        /// <returns>The configuration.</returns>
        public static LoggingConfiguration AddSentry(this LoggingConfiguration configuration,
                                                           string dsn,
                                                           string targetName,
                                                           Action<SentryNLogOptions> optionsConfig = null)
        {
            var options = new SentryNLogOptions();

            optionsConfig?.Invoke(options);

            Target.Register<SentryTarget>("Sentry");

            var target = new SentryTarget(options)
            {
                Name = targetName,
                Layout = "${message}",
            };

            if (dsn != null && options.Dsn == null)
            {
                options.Dsn = new Dsn(dsn);
            }

            configuration?.AddTarget(targetName, target);

            configuration?.AddRuleForAllLevels(targetName);

            return configuration;
        }

        /// <summary>
        /// Add any desired additional tags that will be sent with every message.
        /// </summary>
        /// <param name="options">The options being configured.</param>
        /// <param name="name">   The name of the tag.</param>
        /// <param name="layout"> The layout to be rendered for the tag</param>
        public static void AddTag(this SentryNLogOptions options, string name, Layout layout)
        {
            options.Tags.Add(new TargetPropertyWithContext(name, layout));
        }
    }
}
