using System;
using System.Collections.Generic;

using NLog;
using NLog.Config;
using NLog.Layouts;
using NLog.Targets;

namespace Sentry.NLog
{
    /// <summary>
    /// Sentry Options for NLog logging. All properties can be configured via code or in NLog.config xml file.
    /// </summary>
    /// <inheritdoc />
    [NLogConfigurationItem]
    public class SentryNLogOptions : SentryOptions
    {
        /// <summary>
        /// How many seconds to wait after triggering <see cref="LogManager.Shutdown()"/> before just shutting down the
        /// Sentry sdk.
        /// </summary>
        public int ShutdownTimeoutSeconds
        {
            get => (int)ShutdownTimeout.TotalSeconds;
            set => ShutdownTimeout = TimeSpan.FromSeconds(value);
        }

        /// <summary>
        /// How long to wait for the flush to finish. Defaults to 15 seconds (same as NLog default).
        /// </summary>
        public TimeSpan FlushTimeout { get; set; } = TimeSpan.FromSeconds(15);

        /// <summary>
        /// Minimum log level for events to trigger a send to Sentry. Defaults to <see cref="M:LogLevel.Error" />.
        /// </summary>
        public LogLevel MinimumEventLevel { get; set; } = LogLevel.Error;

        /// <summary>
        /// Minimum log level to be included in the breadcrumb. Defaults to <see cref="M:LogLevel.Info" />.
        /// </summary>
        public LogLevel MinimumBreadcrumbLevel { get; set; } = LogLevel.Info;

        /// <summary>
        /// Set this to <see langword="true" /> to ignore log messages that don't contain an exception.
        /// </summary>
        public bool IgnoreEventsWithNoException { get; set; } = false;

        /// <summary>
        /// Determines whether event properties will be sent to sentry as Tags or not. Defaults to <see langword="false" />.
        /// </summary>
        public bool IncludeEventPropertiesAsTags { get; set; } = false;

        /// <summary>
        /// Determines whether or not to include event-level data as data in breadcrumbs for future errors.
        /// Defaults to <see langword="false" />.
        /// </summary>
        public bool IncludeEventDataOnBreadcrumbs { get; set; } = false;

        /// <summary>
        /// Custom layout for breadcrumbs.
        /// </summary>
        [NLogConfigurationIgnoreProperty] // Configure this directly on the target in XML config.
        public Layout BreadcrumbLayout { get; set; }

        /// <summary>
        /// Custom layout for breadcrumbs category
        /// </summary>
        [NLogConfigurationIgnoreProperty] // Configure this directly on the target in XML config.
        public Layout BreadcrumbCategoryLayout { get; set; }

        /// <summary>
        /// Configured layout for rendering SentryEvent message
        /// </summary>
        [NLogConfigurationIgnoreProperty] // Configure this directly on the target in XML config.
        public Layout Layout { get; set; }

        /// <summary>
        /// Configured layout for Dsn-Address to Sentry
        /// </summary>
        [NLogConfigurationIgnoreProperty] // Configure this directly on the target in XML config.
        public Layout DsnLayout { get; set; }

        /// <summary>
        /// Configured layout for application Release version to Sentry
        /// </summary>
        [NLogConfigurationIgnoreProperty] // Configure this directly on the target in XML config.
        public Layout ReleaseLayout { get; set; }

        /// <summary>
        /// Configured layout for application Environment to Sentry
        /// </summary>
        [NLogConfigurationIgnoreProperty] // Configure this directly on the target in XML config.
        public Layout EnvironmentLayout { get; set; }

        /// <summary>
        /// Any additional tags to apply to each logged message.
        /// </summary>
        [NLogConfigurationIgnoreProperty] // Configure this directly on the target in XML config.
        public IList<TargetPropertyWithContext> Tags { get; } = new List<TargetPropertyWithContext>();

        /// <summary>
        /// Whether the NLog integration should initialize the SDK.
        /// </summary>
        /// <remarks>
        /// By default, if a DSN is provided to the NLog integration it will initialize the SDK.
        /// This might be not ideal when using multiple integrations in case you want another one doing the Init.
        /// </remarks>
        public bool InitializeSdk { get; set; } = true;

        /// <summary>
        /// Optionally configure one or more parts of the user information to be rendered dynamically from an NLog layout
        /// </summary>
        [NLogConfigurationIgnoreProperty] // Configure this directly on the target in XML config.
        public SentryNLogUser User { get; set; } = new SentryNLogUser();
    }
}
