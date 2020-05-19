using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Sentry.Protocol;

namespace Sentry.Extensions.Logging
{
    /// <summary>
    /// Sentry logging integration options
    /// </summary>
    /// <inheritdoc />
    public class SentryLoggingOptions : SentryOptions
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
        /// Defaults tags to add to all events.
        /// </summary>
        /// <remarks>
        /// If the key already exists in the event, it will not be overwritten by a default tag.
        /// </remarks>
        public Dictionary<string, string> DefaultTags { get; } = new Dictionary<string, string>();

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

        /// <summary>
        /// The DSN which defines where events are sent
        /// </summary>
        public new string Dsn
        {
            get => base.Dsn?.ToString();
            set
            {
                if (value != null && !Sentry.Dsn.IsDisabled(value))
                {
                    base.Dsn = new Dsn(value);
                }
            }
        }

        /// <summary>
        /// Whether to initialize this SDK through this integration
        /// </summary>
        public bool InitializeSdk { get; set; } = true;

        /// <summary>
        /// Add a callback to configure the scope upon SDK initialization
        /// </summary>
        /// <param name="action">The function to invoke when initializing the SDK</param>
        public void ConfigureScope(Action<Scope> action) => ConfigureScopeCallbacks = ConfigureScopeCallbacks.Concat(new[] { action }).ToArray();

        /// <summary>
        /// Log entry filters
        /// </summary>
        internal ILogEntryFilter[] Filters { get; set; } = Array.Empty<ILogEntryFilter>();

        /// <summary>
        /// List of callbacks to be invoked when initializing the SDK
        /// </summary>
        internal Action<Scope>[] ConfigureScopeCallbacks { get; set; } = Array.Empty<Action<Scope>>();
    }
}
