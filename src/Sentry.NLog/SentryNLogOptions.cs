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
        /// How many seconds to wait after triggering Logmanager.Shutdown() before just shutting down the
        /// Sentry sdk.
        /// </summary>
        public int ShutdownTimeoutSeconds
        {
            get => ShutdownTimeout.Seconds;
            set => ShutdownTimeout = TimeSpan.FromSeconds(value);
        }

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
        /// Determines whether event-level properties will be sent to sentry as additional data. Defaults to <see langword="true" />.
        /// </summary>
        /// <seealso cref="SendEventPropertiesAsTags" />
        public bool SendEventPropertiesAsData { get; set; } = true;

        /// <summary>
        /// Determines whether event properties will be sent to sentry as Tags or not. Defaults to <see langword="false" />.
        /// </summary>
        public bool SendEventPropertiesAsTags { get; set; } = false;

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
        /// Configured layout for the NLog logger.
        /// </summary>
        [NLogConfigurationIgnoreProperty] // Configure this directly on the target in XML config.
        public Layout Layout { get; set; }

        /// <summary>
        /// Any additional tags to apply to each logged message.
        /// </summary>
        [NLogConfigurationIgnoreProperty] // Configure this directly on the target in XML config.
        public IList<TargetPropertyWithContext> Tags { get; } = new List<TargetPropertyWithContext>();

        [Advanced]
        public bool InitializeSdk { get; set; } = true;
    }
}
