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
        /// Gets or sets the maximum log breadcrumbs.
        /// </summary>
        /// <remarks>
        /// Events with level higher than <see cref="MinimumBreadcrumbLevel"/>
        /// will be logged as <see cref="Breadcrumb"/>
        /// When the number of events reach this configuration value,
        /// older breadcrumbs start dropping to make room for new ones.
        /// </remarks>
        /// <value>
        /// The maximum log breadcrumbs.
        /// </value>
        public int MaxLogBreadcrumbs { get; set; } = 100;

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
    }
}
