using System;
using System.ComponentModel;
using Microsoft.Extensions.Logging;
using Sentry.Protocol;

namespace Sentry.Extensions.Logging
{
    /// <summary>
    /// Sentry logging integration options
    /// </summary>
    public class SentryLoggingOptions
    {
        /// <summary>
        /// Gets or sets the minimum breadcrumb level.
        /// </summary>
        /// <remarks>Events with this level or higher will be stored as <see cref="Breadcrumb"/></remarks>
        /// <value>
        /// The minimum breadcrumb level.
        /// </value>
        public LogLevel MinimumBreadcrumbLevel { get; set; } = LogLevel.Information;

        /// <summary>
        /// Gets or sets the minimum event level.
        /// </summary>
        /// <remarks>
        /// Events with this level or higher will be sent to Sentry
        /// </remarks>
        /// <value>
        /// The minimum event level.
        /// </value>
        public LogLevel MinimumEventLevel { get; set; } = LogLevel.Error;

        public bool InitializeSdk { get; set; } = true;

        // An optional convinience callback to initialize the SDK
        internal Action<SentryOptions> ConfigureOptions { get; private set; }

        /// <summary>
        /// Initializes the SDK: This action should be done only once per application lifetime.
        /// </summary>
        /// <remarks>
        /// Using this initialization method is an alternative to calling <see cref="SentryCore.Init(string)"/> or any overload.
        ///
        /// Initializing the SDK multiple times simply means a new instance is set to the static <see cref="SentryCore"/>.
        /// Any scope data like breadcrumbs added up to calling Init will be not be included in future events.
        ///
        /// The caller of Init is responsible for disposing the instance returned. If the SDK is initialized
        /// via this logging integration, the <see cref="SentryLoggerProvider"/> will dispose the SDK when it is itself disposed.
        /// </remarks>
        /// <param name="configureOptions">The configure options.</param>
        public void Init(Action<SentryOptions> configureOptions) => ConfigureOptions = configureOptions;
    }
}
