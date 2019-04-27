using NLog;
using NLog.Config;

namespace Sentry.NLog
{
    /// <summary>
    /// Sentry Options for NLog logging. Can be configured via code or in NLog.config xml file.
    /// <example>
    ///  NLog config file example:
    ///  <code>
    ///&lt;target type="Sentry" name="sentry" dsn="your.dsn.here"&gt;
    ///    &lt;options&gt;
    ///        &lt;environment&gt;Development&lt;/environment&gt;
    ///    &lt;/options&gt;
    ///&lt;/target&gt;
    ///  </code>
    /// </example>
    /// </summary>
    /// <inheritdoc />
    [NLogConfigurationItem]
    public class SentryNLogOptions : SentryOptions
    {
        /// <summary>
        /// Whether to initialize this SDK through this integration
        /// </summary>
        public bool InitializeSdk { get; set; } = true;

        /// <summary>
        /// Minimum log level for events to trigger a send to Sentry. Defaults to <see cref="LogLevel.Error"/>.
        /// </summary>
        public LogLevel MinimumEventLevel { get; set; } = LogLevel.Error;

        /// <summary>
        /// Minimum log level to be included in the breadcrumb. Defaults to <see cref="LogLevel.Info"/>.
        /// </summary>
        public LogLevel MinimumBreadcrumbLevel { get; set; } = LogLevel.Info;

        /// <summary>
        /// Set this to <see langword="true" /> to ignore log messages that don't contain an exception.
        /// </summary>
        public bool IgnoreEventsWithNoException { get; set; } = false;

        /// <summary>
        /// Determines whether event properties will be sent to sentry as Tags or not. Defaults to <see langword="false" />.
        /// </summary>
        /// <remarks>
        /// If set to <see langword="false" />, event properties will still be sent as additional data unless
        /// <see cref="SendLogEventInfoPropertiesAsData" /> is set to <see langword="false" />.
        /// </remarks>
        /// <seealso cref="SendLogEventInfoPropertiesAsData"/>
        public bool SendLogEventInfoPropertiesAsTags { get; set; } = false;

        /// <summary>
        /// Determines whether event properties will be sent to sentry as additional data. Defaults to <see langword="true" />.
        /// </summary>
        /// <seealso cref="SendLogEventInfoPropertiesAsTags"/>
        public bool SendLogEventInfoPropertiesAsData { get; set; } = true;

        /// <summary>
        /// Determines whether event properties will be sent to sentry as Tags or not. Defaults to <see langword="true" />.
        /// </summary>
        public bool SendContextPropertiesAsTags { get; set; } = true;

    }
}
