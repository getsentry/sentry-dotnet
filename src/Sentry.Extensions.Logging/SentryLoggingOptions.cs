using System.Collections.Generic;
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
        /// Event filters
        /// </summary>
        public IReadOnlyCollection<ILogEventFilter> Filters { get; set; }
    }
}
