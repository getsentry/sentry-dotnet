using System;
using System.Collections.Generic;

using NLog;
using NLog.Config;
using NLog.Targets;

using Sentry.Extensibility;
using Sentry.Infrastructure;

namespace Sentry.NLog
{
    /// <summary>
    /// Sentry Options for NLog logging. All propertiesan be configured via code or in NLog.config xml file.
    /// </summary>
    /// <inheritdoc />
    [NLogConfigurationItem]
    public class SentryNLogOptions : SentryOptions
    {
        /// <summary>
        /// If set to <see langword="true" />, marks namespaces beginning with NLog as external code. Defaults
        /// to <see langword="true" />.
        /// </summary>
        public bool MarkNLogNamespaceAsExternal { get; set; } = true;

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
        /// Minimum log level for events to trigger a send to Sentry. Defaults to <see cref="LogLevel.Error" />.
        /// </summary>
        public LogLevel MinimumEventLevel { get; set; } = LogLevel.Error;

        /// <summary>
        /// Minimum log level to be included in the breadcrumb. Defaults to <see cref="LogLevel.Info" />.
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
        /// Any additional tags to apply to each logged message.
        /// </summary>
        [ArrayParameter(typeof(TargetPropertyWithContext), "tag")]
        public IList<TargetPropertyWithContext> Tags { get; } = new List<TargetPropertyWithContext>();

        [Advanced]
        public bool InitializeSdk { get; set; } = true;

        [Advanced]
        public bool EnableDiagnosticConsoleLogging
        {
            get => _enableDiagnosticsLogging;

            set
            {
                if (value)
                {
                    Debug = true;
                    DiagnosticLogger = _diagnosticsLogger.Value;
                    _enableDiagnosticsLogging = true;
                }
                else if (_enableDiagnosticsLogging)
                {
                    Debug = false;
                    DiagnosticLogger = null;
                    EnableDiagnosticConsoleLogging = false;
                }
            }
        }

        [Advanced]
        public bool EnableDuplicateEventDetection
        {
            get => _enableDuplicateEventDetection;

            set
            {
                if (value == false)
                {
                    this.DisableDuplicateEventDetection();
                }

                _enableDuplicateEventDetection = value;
            }
        }


        private bool _enableDuplicateEventDetection = true;
        private bool _enableDiagnosticsLogging;

        private static readonly Lazy<IDiagnosticLogger> _diagnosticsLogger =
            new Lazy<IDiagnosticLogger>(() => new ConsoleDiagnosticLogger(Protocol.SentryLevel.Debug));

    }
}
